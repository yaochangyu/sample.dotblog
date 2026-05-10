import os
import unittest
from pathlib import Path

from qdrant_client import QdrantClient

from import_1111_jobs_to_qdrant import (
    DEFAULT_COLLECTION_NAME,
    DEFAULT_EMBEDDING_MODEL,
    DEFAULT_QDRANT_URL,
    build_embedding_text,
    build_payload,
    prepare_jobs,
    search_jobs,
    verify_import,
)


FIXTURE_JOB = {
    "jobId": 1,
    "companyId": 2,
    "companyName": "測試公司",
    "title": "會計",
    "description": "處理帳務與報表。",
    "salary": "月薪 32,000元~40,000元",
    "industry": {"name": "電力供應"},
    "workCity": {"name": "彰化縣二林鎮"},
    "jobUrl": "https://www.1111.com.tw/job/1",
    "require": {
        "experienceText": "不拘",
        "gradesDecoded": {"labels": ["大學"], "text": "大學以上"},
        "majorsDecoded": {"text": "不拘"},
        "drivingLicenseDecoded": {"text_labels": ["輕型機車"]},
    },
    "jobPage": {
        "work_time_text": "日班",
        "vacation_text": "週休二日",
        "skills_text": "不拘",
        "welcome_identity_labels": ["歡迎所有求職者"],
        "computer_skill_labels": ["Excel"],
    },
    "enumMeaning": {
        "roleLabels": ["會計／出納／記帳人員"],
        "jobTypeLabels": ["全職"],
    },
    "recruitCount": 1,
    "recruitCountString": "1 人",
}


class TransformTests(unittest.TestCase):
    def test_build_embedding_text_contains_key_fields(self) -> None:
        text = build_embedding_text(FIXTURE_JOB)
        self.assertIn("職缺名稱：會計", text)
        self.assertIn("公司名稱：測試公司", text)
        self.assertIn("工作內容：處理帳務與報表。", text)

    def test_build_payload_contains_required_fields(self) -> None:
        payload = build_payload(FIXTURE_JOB)
        self.assertEqual(payload["job_id"], 1)
        self.assertEqual(payload["company_name"], "測試公司")
        self.assertEqual(payload["industry"], "電力供應")
        self.assertEqual(payload["work_city"], "彰化縣二林鎮")
        self.assertEqual(payload["salary_min"], 32000)
        self.assertEqual(payload["salary_max"], 40000)
        self.assertTrue(payload["document_text"])

    def test_prepare_jobs_unique_ids_and_non_empty_text(self) -> None:
        prepared = prepare_jobs(Path("output/1111_jobs_page1_top100.enriched.json"))
        ids = [job.id for job in prepared]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertTrue(all(job.text for job in prepared))


@unittest.skipUnless(os.getenv("RUN_QDRANT_INTEGRATION") == "1", "integration test disabled")
class QdrantIntegrationTests(unittest.TestCase):
    def test_verify_import_count(self) -> None:
        result = verify_import(
            input_path=Path("output/1111_jobs_page1_top100.enriched.json"),
            qdrant_url=DEFAULT_QDRANT_URL,
            collection_name=DEFAULT_COLLECTION_NAME,
        )
        self.assertEqual(result["actual_count"], result["expected_count"])
        self.assertTrue(result["sample_title"])

    def test_search_with_filter_returns_results(self) -> None:
        results = search_jobs(
            qdrant_url=DEFAULT_QDRANT_URL,
            collection_name=DEFAULT_COLLECTION_NAME,
            model_name=DEFAULT_EMBEDDING_MODEL,
            query="會計 全職 彰化",
            city="彰化縣二林鎮",
            limit=3,
        )
        self.assertTrue(results)

    def test_live_collection_exists(self) -> None:
        client = QdrantClient(url=DEFAULT_QDRANT_URL)
        collections = {item.name for item in client.get_collections().collections}
        self.assertIn(DEFAULT_COLLECTION_NAME, collections)


if __name__ == "__main__":
    unittest.main()
