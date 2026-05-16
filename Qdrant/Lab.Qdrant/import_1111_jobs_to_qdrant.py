#!/usr/bin/env python3
import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from fastembed import TextEmbedding
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


DEFAULT_COLLECTION_NAME = "jobs_1111_page1_top100"
DEFAULT_EMBEDDING_MODEL = "intfloat/multilingual-e5-large"
DEFAULT_QDRANT_URL = "http://localhost:6333"


@dataclass(frozen=True)
class PreparedJob:
    id: int
    text: str
    payload: dict


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


def build_embedding_text(job: dict) -> str:
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


def build_payload(job: dict) -> dict:
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


def prepare_jobs(input_path: Path) -> list[PreparedJob]:
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


def get_embedding_model(model_name: str) -> TextEmbedding:
    return TextEmbedding(model_name=model_name)


def get_vector_size(model_name: str) -> int:
    return int(TextEmbedding.get_embedding_size(model_name))


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
    input_path: Path,
    qdrant_url: str,
    collection_name: str,
    model_name: str,
    recreate: bool = False,
) -> dict:
    prepared_jobs = prepare_jobs(input_path)
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
    input_path: Path,
    qdrant_url: str,
    collection_name: str,
) -> dict:
    prepared_jobs = prepare_jobs(input_path)
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
    query_vector = list(model.query_embed(query))[0].tolist()

    query_filter = None
    if city:
        query_filter = Filter(
            must=[FieldCondition(key="work_city", match=MatchValue(value=city))]
        )

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


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Import 1111 jobs into Qdrant.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--input", type=Path, default=Path("output/1111_jobs_page1_top100.enriched.json"))
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

    subparsers.add_parser("prepare", parents=[common])
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.command == "prepare":
        prepared = prepare_jobs(args.input)
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


if __name__ == "__main__":
    main()
