# 實作計畫：GenerateBlogImages - 根據 blog.md 產生 4 種風格的圖片

本計畫旨在根據 [blog.md](file:///mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md) 的主題（在 8GB VRAM 筆電跑 Gemma 4 並串接 Claude Code），產生 4 種不同藝術風格的精美圖片，做為部落格文章插圖或展示使用。

## 實作步驟

- [x] **步驟 1：產生第一種風格圖片：Cyberpunk / Tech-Noir (科技黑客風)** <!-- id: 1 -->
  - **說明**：使用 `generate_image` 工具生成 `cyberpunk_gemma_claude`。
  - **Prompt 設計**：A high-tech cyberpunk workstation in a dimly lit room. A laptop screen glows intensely with terminal code, system metrics showing VRAM optimization, and neural network graphs. Subtle hologram of a stylized robot (representing Gemma) and a developer tool icon (representing Claude Code) interfacing together. Neon blue, purple, and green accent lighting, highly detailed tech-noir digital art.

- [x] **步驟 2：產生第二種風格圖片：Flat / Vector Illustration (扁平向量插畫)** <!-- id: 2 -->
  - **說明**：使用 `generate_image` 工具生成 `vector_gemma_claude`。
  - **Prompt 設計**：A clean and modern flat vector illustration of a laptop on a minimalist desk. The laptop screen displays optimized gears and a glowing brain, symbolizing local LLM optimization. Abstract visual shapes connect a cute robot head and a modern terminal prompt icon. Harmonious color palette of soft teal, orange, and charcoal gray, sleek tech corporate aesthetic, no text.

- [x] **步驟 3：產生第三種風格圖片：3D Render / Claymation (3D 黏土/渲染風格)** <!-- id: 3 -->
  - **說明**：使用 `generate_image` 工具生成 `claymation_gemma_claude`.png。
  - **Prompt 設計**：Cute 3D claymation scene of a mini laptop that looks slightly overheated with tiny ice cubes on top. A friendly little clay robot is carefully adjusting glowing pipes (representing LiteLLM proxy) connecting to the laptop's GPU. Bright, vibrant colors, soft studio lighting, playful character design, isometric view.

- [x] **步驟 4：產生第四種風格圖片：Retro Pixel Art (復古像素藝術)** <!-- id: 4 -->
  - **說明**：使用 `generate_image` 工具生成 `pixel_art_gemma_claude`.png。
  - **Prompt 設計**：16-bit retro pixel art of a laptop showing a "8GB VRAM: OPTIMIZED" green status bar. A classic game-style avatar of a developer cheering next to the screen. Cybernetic background with glowing datalines, vibrant nostalgic colors, retro arcade game interface look.

- [x] **步驟 5：更新專案結構檔 `@tree.md`** <!-- id: 5 -->
  - **說明**：將產生的 4 張圖片路徑以及 `GenerateBlogImages.plan.md` 寫入 `@tree.md`。

- [x] **步驟 6：封存計畫檔案** <!-- id: 6 -->
  - **說明**：將已完成的計畫檔案移動到 `.archive` 資料夾，並更新 `@tree.md`。
