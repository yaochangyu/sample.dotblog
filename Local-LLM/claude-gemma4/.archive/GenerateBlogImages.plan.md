# 實作計畫：GenerateBlogImages - 根據 blog.md 產生 4 種風格的圖片 (16:9 寬螢幕電影構圖)

本計畫旨在根據 [blog.md](file:///mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md) 的主題，產生 4 種不同藝術風格的 16:9 寬螢幕電影構圖（上下帶有黑邊 letterbox）插圖，以解決生圖工具預設輸出 1:1 的限制，使視覺呈現為橫式寬螢幕。

## 實作步驟

- [x] **步驟 1：產生第一種風格圖片：Cyberpunk / Tech-Noir (16:9 電影構圖科技黑客風)** <!-- id: 1 -->
  - **說明**：使用 `generate_image` 工具生成 `cyberpunk_gemma_claude`。
  - **Prompt 設計**：A high-tech cyberpunk workstation in a dimly lit room. A laptop screen glows intensely with terminal code, system metrics showing VRAM optimization. Subtle hologram of a stylized robot and a developer tool icon interfacing. Neon blue, purple, and green accent lighting, highly detailed tech-noir digital art. Letterbox, cinematic widescreen 16:9 aspect ratio composition with black bars at the top and bottom.

- [x] **步驟 2：產生第二種風格圖片：Flat / Vector Illustration (16:9 電影構圖扁平向量插畫)** <!-- id: 2 -->
  - **說明**：使用 `generate_image` 工具生成 `vector_gemma_claude`。
  - **Prompt 設計**：A clean and modern flat vector illustration of a laptop on a minimalist desk. The laptop screen displays optimized gears and a glowing brain, symbolizing local LLM optimization. Abstract shapes connect a cute robot head and a terminal icon. Harmonious color palette of soft teal, orange, and charcoal gray, sleek corporate tech aesthetic, no text. Letterbox, cinematic widescreen 16:9 aspect ratio composition with black bars at the top and bottom.

- [x] **步驟 3：產生第三種風格圖片：3D Render / Claymation (16:9 電影構圖 3D 黏土風格)** <!-- id: 3 -->
  - **說明**：使用 `generate_image` 工具生成 `claymation_gemma_claude`。
  - **Prompt 設計**：Cute 3D claymation scene of a mini laptop that looks slightly overheated with tiny ice cubes on top. A friendly little clay robot is carefully adjusting glowing pipes connecting to the laptop's GPU. Bright, vibrant colors, soft studio lighting, playful character design, isometric view. Letterbox, cinematic widescreen 16:9 aspect ratio composition with black bars at the top and bottom.

- [x] **步驟 4：產生第四種風格圖片：Retro Pixel Art (16:9 電影構圖復古像素藝術)** <!-- id: 4 -->
  - **說明**：使用 `generate_image` 工具生成 `pixel_art_gemma_claude`。
  - **Prompt 設計**：16-bit retro pixel art of a laptop showing a "8GB VRAM: OPTIMIZED" green status bar. A classic game-style avatar of a developer cheering next to the screen. Cybernetic background with glowing datalines, vibrant nostalgic colors. Letterbox, cinematic widescreen 16:9 aspect ratio composition with black bars at the top and bottom.

- [x] **步驟 5：更新專案結構檔 `@tree.md`** <!-- id: 5 -->
  - **說明**：將產生的 4 張圖片路徑以及 `GenerateBlogImages.plan.md` 寫入 `@tree.md`。

- [x] **步驟 6：封存計畫檔案** <!-- id: 6 -->
  - **說明**：將已完成的計畫檔案移動到 `.archive` 資料夾，並更新 `@tree.md`。
