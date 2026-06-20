#!/usr/bin/env python3
import argparse
import ast
import csv
import hashlib
import json
import re
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from fastembed import TextEmbedding
import kagglehub
from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance,
    FieldCondition,
    Filter,
    MatchValue,
    PayloadSchemaType,
    PointStruct,
    VectorParams,
)


DEFAULT_COLLECTION_NAME = "jobs_104_taiwan"
DEFAULT_EMBEDDING_MODEL = "intfloat/multilingual-e5-large"
DEFAULT_QDRANT_URL = "http://localhost:6333"
DEFAULT_KAGGLE_DATASET = "sunny9999/taiwan-104-career-jd"
DEFAULT_KAGGLE_FILENAME = "career job description.csv"


@dataclass(frozen=True)
class PreparedJob:
    id: int | str
    text: str
    payload: dict


@dataclass(frozen=True)
class InteractiveSearchCommand:
    kind: str
    value: str | int | None = None


def join_values(values: Iterable[str] | None) -> str:
    if not values:
        return ""
    cleaned = [str(value).strip() for value in values if str(value).strip()]
    return "、".join(cleaned)


def parse_salary_range(salary_text: str) -> tuple[int | None, int | None]:
    if not salary_text:
        return None, None

    numbers = [int(token.replace(",", "")) for token in re.findall(r"[\d,]+", salary_text)]
    if not numbers:
        return None, None
    if len(numbers) == 1:
        return numbers[0], numbers[0]
    return min(numbers), max(numbers)


def split_labels(value: str | None) -> list[str]:
    if not value:
        return []
    stripped = value.strip()
    if stripped.startswith("[") and stripped.endswith("]"):
        try:
            parsed = ast.literal_eval(stripped)
        except (SyntaxError, ValueError):
            parsed = None
        if isinstance(parsed, (list, tuple)):
            return [str(item).strip() for item in parsed if str(item).strip()]
    return [item.strip() for item in re.split(r"[、,/，|]", value) if item.strip()]


def build_embedding_text_from_1111(job: dict) -> str:
    require = job.get("require", {})
    job_page = job.get("jobPage", {})
    enum_meaning = job.get("enumMeaning", {})

    sections = [
        ("職缺名稱", job.get("title")),
        ("公司名稱", job.get("companyName")),
        ("產業", job.get("industry", {}).get("name")),
        ("工作地點", job.get("workCity", {}).get("name") or job_page.get("work_location_text")),
        ("職務類別", join_values(enum_meaning.get("roleLabels"))),
        ("工作性質", join_values(enum_meaning.get("jobTypeLabels"))),
        ("工作時間", job_page.get("work_time_text")),
        ("休假制度", job_page.get("vacation_text")),
        ("待遇", job.get("salary")),
        ("學歷要求", require.get("gradesDecoded", {}).get("text")),
        ("工作經驗", require.get("experienceText")),
        ("科系要求", require.get("majorsDecoded", {}).get("text")),
        (
            "駕照要求",
            join_values(require.get("drivingLicenseDecoded", {}).get("text_labels")),
        ),
        ("工作技能", job_page.get("skills_text")),
        ("電腦專長", join_values(job_page.get("computer_skill_labels"))),
        ("歡迎身份", join_values(job_page.get("welcome_identity_labels"))),
        ("工作內容", job.get("description")),
    ]

    lines = [f"{label}：{value}" for label, value in sections if value]
    return "\n".join(lines)


def build_embedding_text_from_kaggle(job: dict) -> str:
    sections = [
        ("職缺類別", job.get("職缺類別")),
        ("職位類別", job.get("職位類別")),
        ("職位", job.get("職位")),
        ("縣市", job.get("縣市")),
        ("地區", job.get("地區")),
        ("公司名稱", job.get("公司名稱")),
        ("職缺名稱", job.get("職缺名稱")),
        ("工作內容", job.get("工作內容")),
        ("職務類別", join_values(split_labels(job.get("職務類別")))),
        ("工作待遇", job.get("工作待遇")),
        ("工作性質", join_values(split_labels(job.get("工作性質")))),
        ("上班地點", job.get("上班地點")),
        ("管理責任", job.get("管理責任")),
        ("上班時段", job.get("上班時段")),
        ("需求人數", job.get("需求人數")),
        ("工作經歷", job.get("工作經歷")),
        ("學歷要求", job.get("學歷要求")),
        ("科系要求", job.get("科系要求")),
        ("擅長工具", join_values(split_labels(job.get("擅長工具")))),
        ("工作技能", job.get("工作技能")),
        ("其他條件", job.get("其他條件")),
        ("公司標籤", join_values(split_labels(job.get("公司標籤")))),
    ]

    lines = [f"{label}：{value}" for label, value in sections if value]
    return "\n".join(lines)


def build_embedding_text(job: dict) -> str:
    if "jobId" in job:
        return build_embedding_text_from_1111(job)
    if "職缺名稱" in job:
        return build_embedding_text_from_kaggle(job)
    raise ValueError("Unsupported job format.")


def build_payload_from_1111(job: dict) -> dict:
    require = job.get("require", {})
    job_page = job.get("jobPage", {})
    enum_meaning = job.get("enumMeaning", {})
    salary_min, salary_max = parse_salary_range(job.get("salary", ""))

    return {
        "job_id": int(job["jobId"]),
        "company_id": int(job["companyId"]),
        "company_name": job.get("companyName"),
        "title": job.get("title"),
        "description": job.get("description"),
        "industry": job.get("industry", {}).get("name"),
        "work_city": job.get("workCity", {}).get("name"),
        "salary_text": job.get("salary"),
        "salary_min": salary_min,
        "salary_max": salary_max,
        "job_url": job.get("jobUrl"),
        "role_labels": enum_meaning.get("roleLabels", []),
        "job_type_labels": enum_meaning.get("jobTypeLabels", []),
        "experience_text": require.get("experienceText"),
        "education_labels": require.get("gradesDecoded", {}).get("labels", []),
        "education_text": require.get("gradesDecoded", {}).get("text"),
        "major_text": require.get("majorsDecoded", {}).get("text"),
        "driving_license_labels": require.get("drivingLicenseDecoded", {}).get("text_labels", []),
        "work_time_text": job_page.get("work_time_text"),
        "vacation_text": job_page.get("vacation_text"),
        "recruit_count": job.get("recruitCount"),
        "recruit_count_text": job.get("recruitCountString"),
        "welcome_identity_labels": job_page.get("welcome_identity_labels", []),
        "computer_skill_labels": job_page.get("computer_skill_labels", []),
        "document_text": build_embedding_text(job),
    }


def build_kaggle_job_id(job: dict, row_number: int) -> int:
    raw = "|".join(
        [
            job.get("公司名稱", ""),
            job.get("職缺名稱", ""),
            job.get("上班地點", ""),
            str(row_number),
        ]
    )
    digest = hashlib.sha1(raw.encode("utf-8")).digest()
    return int.from_bytes(digest[:8], "big") & ((1 << 63) - 1)


def build_payload_from_kaggle(job: dict) -> dict:
    salary_min, salary_max = parse_salary_range(job.get("工作待遇", ""))

    return {
        "job_category": job.get("職缺類別"),
        "position_category": job.get("職位類別"),
        "position": job.get("職位"),
        "company_name": job.get("公司名稱"),
        "title": job.get("職缺名稱"),
        "description": job.get("工作內容"),
        "industry": job.get("職缺類別"),
        "work_city": job.get("縣市"),
        "work_district": job.get("地區"),
        "salary_text": job.get("工作待遇"),
        "salary_min": salary_min,
        "salary_max": salary_max,
        "job_url": None,
        "role_labels": split_labels(job.get("職務類別")),
        "job_type_labels": split_labels(job.get("工作性質")),
        "experience_text": job.get("工作經歷"),
        "education_labels": split_labels(job.get("學歷要求")),
        "education_text": job.get("學歷要求"),
        "major_text": job.get("科系要求"),
        "driving_license_labels": [],
        "work_time_text": job.get("上班時段"),
        "vacation_text": None,
        "recruit_count": None,
        "recruit_count_text": job.get("需求人數"),
        "welcome_identity_labels": [],
        "computer_skill_labels": split_labels(job.get("擅長工具")),
        "document_text": build_embedding_text(job),
        "job_location": job.get("上班地點"),
        "management_responsibility": job.get("管理責任"),
        "demand_supply_ratio": job.get("供需人數"),
        "skills_text": job.get("工作技能"),
        "other_conditions": job.get("其他條件"),
        "capital_amount": job.get("資本額"),
        "employee_count": job.get("員工人數"),
        "company_tags": split_labels(job.get("公司標籤")),
    }


def build_payload(job: dict) -> dict:
    if "jobId" in job:
        return build_payload_from_1111(job)
    if "職缺名稱" in job:
        return build_payload_from_kaggle(job)
    raise ValueError("Unsupported job format.")


def download_kaggle_dataset(
    dataset_name: str = DEFAULT_KAGGLE_DATASET,
    filename: str = DEFAULT_KAGGLE_FILENAME,
) -> Path:
    dataset_dir = Path(kagglehub.dataset_download(dataset_name))
    dataset_path = dataset_dir / filename
    if dataset_path.exists():
        return dataset_path

    csv_files = sorted(dataset_dir.glob("*.csv"))
    if csv_files:
        return csv_files[0]

    raise FileNotFoundError(f"No CSV file found in dataset: {dataset_name}")


def prepare_jobs_from_json(input_path: Path) -> list[PreparedJob]:
    payload = json.loads(input_path.read_text(encoding="utf-8"))
    jobs = payload["jobs"]
    prepared: list[PreparedJob] = []

    for job in jobs:
        prepared.append(
            PreparedJob(
                id=int(job["jobId"]),
                text=build_embedding_text(job),
                payload=build_payload(job),
            )
        )

    return prepared


def prepare_jobs_from_csv(input_path: Path) -> list[PreparedJob]:
    prepared: list[PreparedJob] = []

    with input_path.open("r", encoding="utf-8-sig", newline="") as file:
        reader = csv.DictReader(file)
        for row_number, row in enumerate(reader, start=1):
            job_id = build_kaggle_job_id(row, row_number)
            payload = build_payload(row)
            payload["job_id"] = job_id
            prepared.append(
                PreparedJob(
                    id=job_id,
                    text=build_embedding_text(row),
                    payload=payload,
                )
            )

    return prepared


def prepare_jobs(
    input_path: Path | None = None,
    dataset_name: str = DEFAULT_KAGGLE_DATASET,
) -> list[PreparedJob]:
    resolved_input = input_path or download_kaggle_dataset(dataset_name=dataset_name)
    if resolved_input.suffix.lower() == ".json":
        return prepare_jobs_from_json(resolved_input)
    if resolved_input.suffix.lower() == ".csv":
        return prepare_jobs_from_csv(resolved_input)
    raise ValueError(f"Unsupported input format: {resolved_input.suffix}")


def get_embedding_model(model_name: str) -> TextEmbedding:
    return TextEmbedding(model_name=model_name)


def get_vector_size(model_name: str) -> int:
    return int(TextEmbedding.get_embedding_size(model_name))


def build_query_filter(city: str | None) -> Filter | None:
    if not city:
        return None
    return Filter(must=[FieldCondition(key="work_city", match=MatchValue(value=city))])


def ensure_collection(
    client: QdrantClient,
    collection_name: str,
    vector_size: int,
    recreate: bool = False,
) -> None:
    existing = {item.name for item in client.get_collections().collections}

    if recreate and collection_name in existing:
        client.delete_collection(collection_name)
        existing.remove(collection_name)

    if collection_name not in existing:
        client.create_collection(
            collection_name=collection_name,
            vectors_config=VectorParams(size=vector_size, distance=Distance.COSINE),
        )

    for field_name, schema in [
        ("job_id", PayloadSchemaType.INTEGER),
        ("company_name", PayloadSchemaType.KEYWORD),
        ("industry", PayloadSchemaType.KEYWORD),
        ("work_city", PayloadSchemaType.KEYWORD),
        ("role_labels", PayloadSchemaType.KEYWORD),
        ("job_type_labels", PayloadSchemaType.KEYWORD),
        ("salary_min", PayloadSchemaType.INTEGER),
        ("salary_max", PayloadSchemaType.INTEGER),
    ]:
        client.create_payload_index(
            collection_name=collection_name,
            field_name=field_name,
            field_schema=schema,
        )


def import_jobs(
    *,
    input_path: Path | None,
    dataset_name: str,
    qdrant_url: str,
    collection_name: str,
    model_name: str,
    recreate: bool = False,
) -> dict:
    prepared_jobs = prepare_jobs(input_path, dataset_name)
    model = get_embedding_model(model_name)
    vectors = list(model.embed([job.text for job in prepared_jobs]))
    vector_size = len(vectors[0]) if vectors else get_vector_size(model_name)

    client = QdrantClient(url=qdrant_url)
    ensure_collection(client, collection_name, vector_size, recreate=recreate)

    points = [
        PointStruct(id=job.id, vector=vector.tolist(), payload=job.payload)
        for job, vector in zip(prepared_jobs, vectors, strict=True)
    ]
    client.upsert(collection_name=collection_name, points=points, wait=True)

    return {
        "count": len(points),
        "collection_name": collection_name,
        "vector_size": vector_size,
        "sample_job_id": prepared_jobs[0].id if prepared_jobs else None,
    }


def verify_import(
    *,
    input_path: Path | None,
    dataset_name: str,
    qdrant_url: str,
    collection_name: str,
) -> dict:
    prepared_jobs = prepare_jobs(input_path, dataset_name)
    client = QdrantClient(url=qdrant_url)
    count_result = client.count(collection_name=collection_name, exact=True)
    sample_id = prepared_jobs[0].id
    sample_points = client.retrieve(
        collection_name=collection_name,
        ids=[sample_id],
        with_payload=True,
        with_vectors=False,
    )
    sample_payload = sample_points[0].payload if sample_points else {}

    return {
        "expected_count": len(prepared_jobs),
        "actual_count": count_result.count,
        "sample_job_id": sample_id,
        "sample_title": sample_payload.get("title"),
        "sample_city": sample_payload.get("work_city"),
    }


def search_jobs(
    *,
    qdrant_url: str,
    collection_name: str,
    model_name: str,
    query: str,
    city: str | None = None,
    limit: int = 5,
) -> list[dict]:
    client = QdrantClient(url=qdrant_url)
    model = get_embedding_model(model_name)
    return search_jobs_with_client(
        client=client,
        model=model,
        collection_name=collection_name,
        query=query,
        city=city,
        limit=limit,
    )


def search_jobs_with_client(
    *,
    client: QdrantClient,
    model: TextEmbedding,
    collection_name: str,
    query: str,
    city: str | None = None,
    limit: int = 5,
) -> list[dict]:
    query_vector = list(model.query_embed(query))[0].tolist()
    query_filter = build_query_filter(city)

    results = client.query_points(
        collection_name=collection_name,
        query=query_vector,
        query_filter=query_filter,
        with_payload=True,
        with_vectors=False,
        limit=limit,
    )
    return [
        {
            "id": point.id,
            "score": point.score,
            "title": point.payload.get("title"),
            "company_name": point.payload.get("company_name"),
            "work_city": point.payload.get("work_city"),
        }
        for point in results.points
    ]


def parse_interactive_search_input(raw_input: str) -> InteractiveSearchCommand:
    stripped = raw_input.strip()
    if not stripped:
        return InteractiveSearchCommand(kind="empty")
    if stripped in {":quit", ":exit"}:
        return InteractiveSearchCommand(kind="quit")
    if stripped == ":show":
        return InteractiveSearchCommand(kind="show")
    if stripped == ":clear-city":
        return InteractiveSearchCommand(kind="clear-city")
    if stripped.startswith(":city "):
        city = stripped.removeprefix(":city ").strip()
        if not city:
            raise ValueError("city command requires a value")
        return InteractiveSearchCommand(kind="set-city", value=city)
    if stripped.startswith(":limit "):
        value = stripped.removeprefix(":limit ").strip()
        limit = int(value)
        if limit <= 0:
            raise ValueError("limit must be greater than 0")
        return InteractiveSearchCommand(kind="set-limit", value=limit)
    if stripped.startswith(":"):
        raise ValueError(f"unsupported command: {stripped}")
    return InteractiveSearchCommand(kind="query", value=stripped)


def interactive_search(
    *,
    qdrant_url: str,
    collection_name: str,
    model_name: str,
    city: str | None = None,
    limit: int = 5,
) -> None:
    client = QdrantClient(url=qdrant_url)
    model = get_embedding_model(model_name)
    collections = {item.name for item in client.get_collections().collections}
    if collection_name not in collections:
        raise ValueError(f"Collection not found: {collection_name}")

    count_result = client.count(collection_name=collection_name, exact=True)
    if count_result.count == 0:
        raise ValueError(f"Collection is empty: {collection_name}")

    current_city = city
    current_limit = limit

    print(f"Interactive search ready: collection={collection_name}, count={count_result.count}")
    print("Commands: :city <縣市>, :clear-city, :limit <N>, :show, :quit")

    while True:
        prompt_city = current_city or "-"
        try:
            raw_input = input(f"query(city={prompt_city}, limit={current_limit})> ")
        except EOFError:
            print("exit")
            return
        except KeyboardInterrupt:
            print("\nexit")
            return

        command = parse_interactive_search_input(raw_input)
        if command.kind == "empty":
            continue
        if command.kind == "quit":
            print("bye")
            return
        if command.kind == "show":
            print(json.dumps({"city": current_city, "limit": current_limit}, ensure_ascii=False))
            continue
        if command.kind == "clear-city":
            current_city = None
            print("city filter cleared")
            continue
        if command.kind == "set-city":
            current_city = str(command.value)
            print(f"city filter set to: {current_city}")
            continue
        if command.kind == "set-limit":
            current_limit = int(command.value)
            print(f"limit set to: {current_limit}")
            continue

        start_time = time.perf_counter()
        results = search_jobs_with_client(
            client=client,
            model=model,
            collection_name=collection_name,
            query=str(command.value),
            city=current_city,
            limit=current_limit,
        )
        elapsed_ms = (time.perf_counter() - start_time) * 1000
        print(json.dumps({"elapsed_ms": round(elapsed_ms, 2), "results": results}, ensure_ascii=False, indent=2))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Import job datasets into Qdrant.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--input", type=Path)
    common.add_argument("--dataset", default=DEFAULT_KAGGLE_DATASET)
    common.add_argument("--qdrant-url", default=DEFAULT_QDRANT_URL)
    common.add_argument("--collection", default=DEFAULT_COLLECTION_NAME)
    common.add_argument("--model", default=DEFAULT_EMBEDDING_MODEL)

    import_parser = subparsers.add_parser("import", parents=[common])
    import_parser.add_argument("--recreate", action="store_true")

    subparsers.add_parser("verify", parents=[common])

    search_parser = subparsers.add_parser("search", parents=[common])
    search_parser.add_argument("--query", required=True)
    search_parser.add_argument("--city")
    search_parser.add_argument("--limit", type=int, default=5)

    interactive_search_parser = subparsers.add_parser("interactive-search", parents=[common])
    interactive_search_parser.add_argument("--city")
    interactive_search_parser.add_argument("--limit", type=int, default=5)

    subparsers.add_parser("prepare", parents=[common])
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.command == "prepare":
        prepared = prepare_jobs(args.input, args.dataset)
        print(json.dumps(
            {
                "count": len(prepared),
                "sample": {
                    "id": prepared[0].id,
                    "text": prepared[0].text,
                    "payload": prepared[0].payload,
                } if prepared else None,
            },
            ensure_ascii=False,
            indent=2,
        ))
        return

    if args.command == "import":
        result = import_jobs(
            input_path=args.input,
            dataset_name=args.dataset,
            qdrant_url=args.qdrant_url,
            collection_name=args.collection,
            model_name=args.model,
            recreate=args.recreate,
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return

    if args.command == "verify":
        result = verify_import(
            input_path=args.input,
            dataset_name=args.dataset,
            qdrant_url=args.qdrant_url,
            collection_name=args.collection,
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return

    if args.command == "search":
        results = search_jobs(
            qdrant_url=args.qdrant_url,
            collection_name=args.collection,
            model_name=args.model,
            query=args.query,
            city=args.city,
            limit=args.limit,
        )
        print(json.dumps(results, ensure_ascii=False, indent=2))
        return

    if args.command == "interactive-search":
        interactive_search(
            qdrant_url=args.qdrant_url,
            collection_name=args.collection,
            model_name=args.model,
            city=args.city,
            limit=args.limit,
        )


if __name__ == "__main__":
    main()
