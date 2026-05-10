#!/usr/bin/env python3
import argparse
import json
import re
import time
from pathlib import Path
from urllib.parse import quote

import requests
from bs4 import BeautifulSoup, Tag


API_URL = "https://www.1111.com.tw/api/v1/search/jobs/"
REFERER_TEMPLATE = "https://www.1111.com.tw/search/job?page={page}&col=da&sort=desc"
JOB_URL_TEMPLATE = "https://www.1111.com.tw/job/{job_id}"
USER_AGENT = "Mozilla/5.0"

GRADE_CODE_LABELS = {
    "1": "國中以下",
    "2": "高中職",
    "8": "專科",
    "16": "大學",
    "32": "碩士",
    "64": "博士",
}

DRIVING_LICENSE_BITS = {
    2048: "手排小型車",
    64: "普通重機車",
    32: "職業聯結車",
    16: "職業大客車",
    8: "職業大貨車",
    4: "職業小型車",
    2: "自排小型車",
    1: "輕型機車",
}


# --- fetch ---

def build_url(page: int, offset: int) -> str:
    search_url = quote(f"/search/job?page={page}&col=da&sort=desc", safe="")
    return (
        f"{API_URL}?page={page}"
        f"&fromOffset={offset}"
        "&sortBy=da"
        "&sortOrder=desc"
        "&conditionsText"
        f"&searchUrl={search_url}"
        "&isSyncedRecommendJobs=true"
    )


def fetch_batch(session: requests.Session, page: int, offset: int) -> list[dict]:
    response = session.get(
        build_url(page, offset),
        headers={
            "Accept": "*/*",
            "Referer": REFERER_TEMPLATE.format(page=page),
            "User-Agent": USER_AGENT,
        },
        timeout=30,
    )
    response.raise_for_status()
    payload = response.json()
    result = payload.get("result", payload)
    return result.get("hits", [])


def collect_jobs(session: requests.Session, target_count: int, start_page: int) -> list[dict]:
    jobs: list[dict] = []
    seen_ids: set[int] = set()
    tried_keys: set[tuple[int, int]] = set()
    consecutive_duplicates = 0
    page = start_page
    offset = 0

    while len(jobs) < target_count and consecutive_duplicates < 12:
        key = (page, offset)
        if key in tried_keys:
            page += 1
            continue

        tried_keys.add(key)
        hits = fetch_batch(session, page, offset)
        new_count = 0

        for hit in hits:
            job_id = hit.get("jobId")
            if job_id in seen_ids:
                continue
            seen_ids.add(job_id)
            jobs.append(hit)
            new_count += 1
            if len(jobs) >= target_count:
                break

        if new_count == 0:
            consecutive_duplicates += 1
        else:
            consecutive_duplicates = 0

        offset += 10
        if offset > 110:
            page += 1
            offset = 0

        time.sleep(0.2)

    return jobs[:target_count]


# --- enrich ---

def normalize_whitespace(text: str) -> str:
    return " ".join(text.split())


def split_display_values(text: str) -> list[str]:
    cleaned = normalize_whitespace(text)
    if not cleaned:
        return []
    values = [
        value.strip()
        for value in re.split(r"\s*[、｜|]\s*", cleaned)
        if value.strip() and value.strip() != "、"
    ]
    return values or [cleaned]


def find_labeled_section(soup: BeautifulSoup, label: str) -> Tag | None:
    return soup.find(
        lambda tag: isinstance(tag, Tag)
        and tag.name == "h3"
        and tag.get_text(strip=True) == label
    )


def extract_label_value(soup: BeautifulSoup, label: str, as_list: bool = False) -> str | list[str] | None:
    heading = find_labeled_section(soup, label)
    if heading is None:
        return None

    values: list[str] = []
    for child in heading.parent.children:
        if not isinstance(child, Tag) or child is heading:
            continue
        text = normalize_whitespace(child.get_text(" ", strip=True))
        if not text:
            continue
        values.extend(split_display_values(text) if as_list else [text])

    if not values:
        return None
    if as_list:
        deduped: list[str] = []
        for value in values:
            if value not in deduped:
                deduped.append(value)
        return deduped
    return " ".join(values)


def fetch_job_page(session: requests.Session, job_id: int) -> dict:
    response = session.get(
        JOB_URL_TEMPLATE.format(job_id=job_id),
        headers={"User-Agent": USER_AGENT},
        timeout=30,
    )
    response.raise_for_status()
    soup = BeautifulSoup(response.text, "html.parser")

    return {
        "job_url": JOB_URL_TEMPLATE.format(job_id=job_id),
        "salary_text": extract_label_value(soup, "工作待遇"),
        "role_labels": extract_label_value(soup, "職務類別", as_list=True),
        "work_type_labels": extract_label_value(soup, "工作性質", as_list=True),
        "work_time_text": extract_label_value(soup, "工作時間"),
        "vacation_text": extract_label_value(soup, "休假制度"),
        "work_location_text": extract_label_value(soup, "工作地點"),
        "education_text": extract_label_value(soup, "學歷要求"),
        "experience_text": extract_label_value(soup, "工作經驗"),
        "language_text": extract_label_value(soup, "外語能力"),
        "skills_text": extract_label_value(soup, "工作技能"),
        "welcome_identity_labels": extract_label_value(soup, "歡迎身份", as_list=True),
        "industry_text": extract_label_value(soup, "產業類別"),
        "major_text": extract_label_value(soup, "科系要求"),
        "driving_license_labels": extract_label_value(soup, "具備駕照", as_list=True),
        "recruit_count_text": extract_label_value(soup, "需求人數"),
        "computer_skill_labels": extract_label_value(soup, "電腦專長", as_list=True),
    }


def decode_experience(code: str, page_text: str | None) -> str | None:
    if page_text:
        return page_text
    if code == "0":
        return "不拘"
    if code.isdigit() and int(code) >= 3:
        return f"{int(code) - 2} 年以上經驗"
    return None


def decode_grades(codes: list[str], page_text: str | None) -> dict:
    return {
        "labels": [GRADE_CODE_LABELS.get(code, f"未知學歷代碼:{code}") for code in codes],
        "text": page_text,
    }


def decode_driving_license(codes: list[str], page_labels: list[str] | None) -> dict:
    decoded: list[dict] = []
    for code in codes:
        if code == "0":
            decoded.append({"code": code, "labels": []})
            continue
        numeric = int(code)
        labels = [label for bit, label in DRIVING_LICENSE_BITS.items() if numeric & bit]
        decoded.append({"code": code, "labels": labels})

    return {
        "items": decoded,
        "text_labels": page_labels or [],
    }


def decode_majors(codes: list[str], page_labels: list[str] | None, page_text: str | None) -> dict:
    labels = page_labels or []
    mapped_items: list[dict] = []

    if len(codes) == len(labels):
        mapped_items = [{"code": code, "label": label} for code, label in zip(codes, labels)]
    elif len(codes) == 1 and len(labels) == 1:
        mapped_items = [{"code": codes[0], "label": labels[0]}]

    return {
        "items": mapped_items,
        "text": page_text,
    }


def infer_internship_labels(job: dict, page_data: dict) -> list[str]:
    work_type_labels = page_data.get("work_type_labels") or []
    return [label for label in work_type_labels if "實習" in label]


def enrich_job(job: dict, page_data: dict) -> dict:
    enriched = dict(job)
    require = dict(job.get("require", {}))
    grade_codes = [str(code) for code in require.get("grades", [])]
    major_codes = [str(code) for code in require.get("majors", []) if str(code) != "0"]
    driving_codes = [str(code) for code in require.get("drivingLicense", [])]

    require["experienceText"] = decode_experience(str(require.get("experience", "")), page_data.get("experience_text"))
    require["gradesDecoded"] = decode_grades(grade_codes, page_data.get("education_text"))
    require["majorsDecoded"] = decode_majors(
        major_codes,
        page_data.get("major_text").split(" 、 ") if page_data.get("major_text") and " 、 " in page_data["major_text"] else split_display_values(page_data["major_text"]) if page_data.get("major_text") else [],
        page_data.get("major_text"),
    )
    require["drivingLicenseDecoded"] = decode_driving_license(driving_codes, page_data.get("driving_license_labels"))

    enriched["require"] = require
    enriched["jobUrl"] = page_data["job_url"]
    enriched["jobPage"] = page_data
    enriched["enumMeaning"] = {
        "roleLabels": page_data.get("role_labels") or [],
        "jobTypeLabels": page_data.get("work_type_labels") or [],
        "internshipLabels": infer_internship_labels(job, page_data),
        "benefitCodesUnresolved": [str(code) for code in job.get("benefits", []) if str(code) != "0"],
        "companyTagCodesUnresolved": [str(code) for code in job.get("companyTags", [])],
        "remindCodesUnresolved": [str(code) for code in job.get("remind", [])],
    }
    return enriched


# --- main ---

def main() -> None:
    parser = argparse.ArgumentParser(description="Fetch and enrich jobs from 1111.")
    parser.add_argument("--count", type=int, default=100, help="Number of jobs to fetch.")
    parser.add_argument("--start-page", type=int, default=1, help="Search page to start from.")
    parser.add_argument("--delay", type=float, default=0.2, help="Delay between enrich requests in seconds.")
    parser.add_argument("--output", type=Path, default=None, help="Output JSON file path.")
    args = parser.parse_args()

    if args.output is None:
        args.output = Path("output") / f"1111_jobs_page{args.start_page}_top{args.count}.enriched.json"

    args.output.parent.mkdir(parents=True, exist_ok=True)

    with requests.Session() as session:
        jobs = collect_jobs(session, target_count=args.count, start_page=args.start_page)
        print(f"fetched {len(jobs)} jobs")

        enriched_jobs: list[dict] = []
        for index, job in enumerate(jobs, start=1):
            page_data = fetch_job_page(session, int(job["jobId"]))
            enriched_jobs.append(enrich_job(job, page_data))
            print(f"enriched {index}/{len(jobs)}", end="\r")
            if index != len(jobs):
                time.sleep(args.delay)

    print()
    output = {
        "source_url": REFERER_TEMPLATE.format(page=args.start_page),
        "count": len(enriched_jobs),
        "jobs": enriched_jobs,
        "notes": {
            "resolved_from_job_page": [
                "role", "jobType", "require.experience",
                "require.grades", "require.majors", "require.drivingLicense", "industry",
            ],
            "partially_resolved_from_job_page": ["internship", "mrtId", "mrtTime", "mrtNear"],
            "still_unresolved_codes": ["benefits", "companyTags", "remind", "require.certificates"],
        },
    }
    args.output.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"saved {len(enriched_jobs)} enriched jobs to {args.output}")


if __name__ == "__main__":
    main()
