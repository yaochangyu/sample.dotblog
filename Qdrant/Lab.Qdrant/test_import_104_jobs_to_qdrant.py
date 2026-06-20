import csv
import os
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock, patch

from qdrant_client import QdrantClient

from import_104_jobs_to_qdrant import (
    DEFAULT_COLLECTION_NAME,
    DEFAULT_KAGGLE_DATASET,
    DEFAULT_EMBEDDING_MODEL,
    DEFAULT_QDRANT_URL,
    build_embedding_text,
    build_payload,
    build_query_filter,
    download_kaggle_dataset,
    parse_interactive_search_input,
    prepare_jobs,
    search_jobs,
    search_jobs_with_client,
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

FIXTURE_KAGGLE_JOB = {
    "職缺類別": "資訊軟體系統類",
    "職位類別": "軟體_工程類人員",
    "職位": "資料工程師",
    "縣市": "臺北市",
    "地區": "中正區",
    "供需人數": "2.5",
    "公司名稱": "測試科技股份有限公司",
    "職缺名稱": "資料工程師",
    "工作內容": "維護資料管線與分析平台。",
    "職務類別": "資料工程師、後端工程師",
    "工作待遇": "月薪 60,000元~80,000元",
    "工作性質": "全職",
    "上班地點": "臺北市中正區忠孝西路100號",
    "管理責任": "不需負擔管理責任",
    "上班時段": "日班",
    "需求人數": "1人",
    "工作經歷": "3年以上",
    "學歷要求": "大學以上",
    "科系要求": "資訊工程相關",
    "擅長工具": "Python、SQL",
    "工作技能": "ETL、Airflow",
    "其他條件": "熟悉雲端服務",
    "資本額": "1000萬元",
    "員工人數": "50人",
    "公司標籤": "遠端、彈性工時",
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

    def test_build_embedding_text_from_kaggle_contains_key_fields(self) -> None:
        text = build_embedding_text(FIXTURE_KAGGLE_JOB)
        self.assertIn("職缺名稱：資料工程師", text)
        self.assertIn("公司名稱：測試科技股份有限公司", text)
        self.assertIn("工作內容：維護資料管線與分析平台。", text)

    def test_build_payload_from_kaggle_contains_required_fields(self) -> None:
        payload = build_payload(FIXTURE_KAGGLE_JOB)
        self.assertEqual(payload["company_name"], "測試科技股份有限公司")
        self.assertEqual(payload["title"], "資料工程師")
        self.assertEqual(payload["work_city"], "臺北市")
        self.assertEqual(payload["salary_min"], 60000)
        self.assertEqual(payload["salary_max"], 80000)
        self.assertEqual(payload["computer_skill_labels"], ["Python", "SQL"])
        self.assertTrue(payload["document_text"])

    def test_prepare_jobs_from_csv_unique_ids_and_non_empty_text(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            csv_path = Path(temp_dir) / "jobs.csv"
            with csv_path.open("w", encoding="utf-8-sig", newline="") as file:
                writer = csv.DictWriter(file, fieldnames=list(FIXTURE_KAGGLE_JOB))
                writer.writeheader()
                writer.writerow(FIXTURE_KAGGLE_JOB)
                writer.writerow(
                    FIXTURE_KAGGLE_JOB | {"職缺名稱": "後端工程師", "公司名稱": "另一家測試公司"}
                )

            prepared = prepare_jobs(csv_path)

        ids = [job.id for job in prepared]
        self.assertEqual(len(prepared), 2)
        self.assertEqual(len(ids), len(set(ids)))
        self.assertTrue(all(job.text for job in prepared))

    @patch("import_104_jobs_to_qdrant.kagglehub.dataset_download")
    def test_download_kaggle_dataset_returns_csv_path(self, mock_dataset_download) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            csv_path = Path(temp_dir) / "career job description.csv"
            with csv_path.open("w", encoding="utf-8-sig", newline="") as file:
                writer = csv.DictWriter(file, fieldnames=list(FIXTURE_KAGGLE_JOB))
                writer.writeheader()
                writer.writerow(FIXTURE_KAGGLE_JOB)

            mock_dataset_download.return_value = temp_dir

            resolved = download_kaggle_dataset(DEFAULT_KAGGLE_DATASET)

        self.assertEqual(resolved, csv_path)

    def test_build_query_filter_returns_none_without_city(self) -> None:
        self.assertIsNone(build_query_filter(None))

    def test_build_query_filter_returns_filter_with_city(self) -> None:
        query_filter = build_query_filter("臺北市")
        self.assertIsNotNone(query_filter)
        self.assertEqual(query_filter.must[0].key, "work_city")
        self.assertEqual(query_filter.must[0].match.value, "臺北市")

    def test_parse_interactive_search_input_parses_commands(self) -> None:
        self.assertEqual(parse_interactive_search_input(":quit").kind, "quit")
        self.assertEqual(parse_interactive_search_input(":show").kind, "show")
        self.assertEqual(parse_interactive_search_input(":clear-city").kind, "clear-city")
        self.assertEqual(parse_interactive_search_input(":city 臺北市").value, "臺北市")
        self.assertEqual(parse_interactive_search_input(":limit 7").value, 7)
        self.assertEqual(parse_interactive_search_input("軟體工程師 台北").kind, "query")

    def test_parse_interactive_search_input_rejects_bad_command(self) -> None:
        with self.assertRaises(ValueError):
            parse_interactive_search_input(":unknown")

        with self.assertRaises(ValueError):
            parse_interactive_search_input(":limit 0")

    def test_search_jobs_with_client_reuses_supplied_model_and_client(self) -> None:
        fake_model = Mock()
        fake_vector = Mock()
        fake_vector.tolist.return_value = [0.1, 0.2, 0.3]
        fake_model.query_embed.return_value = iter([fake_vector])

        fake_client = Mock()
        fake_client.query_points.return_value = SimpleNamespace(
            points=[
                SimpleNamespace(
                    id=123,
                    score=0.99,
                    payload={
                        "title": "資料工程師",
                        "company_name": "測試科技",
                        "work_city": "臺北市",
                    },
                )
            ]
        )

        results = search_jobs_with_client(
            client=fake_client,
            model=fake_model,
            collection_name="jobs_104_smoke",
            query="資料工程師 台北",
            city="臺北市",
            limit=3,
        )

        fake_model.query_embed.assert_called_once_with("資料工程師 台北")
        fake_client.query_points.assert_called_once()
        self.assertEqual(results[0]["title"], "資料工程師")


@unittest.skipUnless(os.getenv("RUN_QDRANT_INTEGRATION") == "1", "integration test disabled")
class QdrantIntegrationTests(unittest.TestCase):
    def test_verify_import_count(self) -> None:
        result = verify_import(
            input_path=Path("output/1111_jobs_page1_top100.enriched.json"),
            dataset_name=DEFAULT_KAGGLE_DATASET,
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
