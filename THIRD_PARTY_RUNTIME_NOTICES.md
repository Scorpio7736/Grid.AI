# Bundled runtime notices

- Optional llama.cpp CPU provider verification uses release `b12110`. The official Windows CPU archive SHA256 is `98901E327C252875ACCAF2D1BF364E10E868843B0B63468BAAD1AA945CDC5DA2`. Retain `runtime/llamacpp/b12110/LICENSE` and `LICENSE-LLVM-OpenMP` when packaging this runtime. A copy of the upstream license also ships at `app/licenses/llama.cpp-LICENSE`. [Pinned upstream source](https://github.com/ggml-org/llama.cpp/tree/b12110).

- PdfPig 0.1.16 supplies offline PDF text extraction. Its managed assemblies ship with the terminal and are included in portable hash/size inventories. The upstream license, including bundled-component notices, ships at `app/licenses/PdfPig-LICENSE`. Source revision `a7bb35662bbbf405efddad50aedc9bcdcf515afc` is recorded by the NuGet package. [Upstream source and license](https://github.com/UglyToad/PdfPig/tree/a7bb35662bbbf405efddad50aedc9bcdcf515afc).

Portable GRID contains third-party components; it is not dependency-free source code.

- .NET runtime: self-contained publish includes the Microsoft runtime and its supplied notices/licenses.
- Ollama: MIT license; preserve the license from the runtime distribution when redistributing. Official source: https://github.com/ollama/ollama/blob/main/LICENSE
- Ollama native libraries/GPU components: preserve the distribution's included notices and comply with their individual redistribution terms. The provisioning script copies the complete library directory.
- Qwen3-4B model: Apache-2.0. The model's license is retained in the Ollama model license blob. Official source: https://huggingface.co/Qwen/Qwen3-4B/blob/main/LICENSE

Provisioning is a one-time installation/build step. Binary runtime/model assets are deliberately not committed to Git. A source clone needs provisioning; a prepared release includes those assets and runs local inference without downloading anything. Public redistribution requires reviewing the actual supplied licenses, especially GPU libraries; a successful local build is not a legal redistribution audit.

- Retrieval reuses Microsoft.Data.Sqlite.Core 10.0.13 (MIT package metadata) and SQLitePCLRaw bundle/native e_sqlite3 2.1.13 (Apache-2.0 package metadata). No additional vector extension is bundled; exact vector ranking is managed GRID code. Preserve the SQLite stack distribution notices with portable packages.
