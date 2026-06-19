# 移除 Co-authored-by 計畫執行問題紀錄

## 失敗的嘗試

### 1. 使用 git filter-branch 搭配 sed 移除不完全
- **步驟**：步驟 2 - 改寫歷史移除 Co-authored-by
- **指令**：`FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch -f --msg-filter 'sed "/[Cc]o-authored-by/d"' HEAD`
- **原因**：Commit Message 中的 `Co-Authored-By` 含有大寫字母 `A`（`Co-Authored-By`），而使用的正則表達式 `/[Cc]o-authored-by/d` 中的 `authored` 為全小寫，導致無法正確匹配並刪除該行。
- **後續改進**：應使用大小寫無關的匹配（如 `sed` 的 `I` 標記，或同時考慮大寫的 `Co-Authored-By`），例如 `sed "/[Cc]o-[Aa]uthored-[Bb]y/d"` 或 `sed -E "/[Cc]o-[Aa]uthored-[Bb]y/d"`。
