# Bundled runtime notices

Portable GRID contains third-party components; it is not dependency-free source code.

- .NET runtime: self-contained publish includes the Microsoft runtime and its supplied notices/licenses.
- Ollama: MIT license; preserve the license from the runtime distribution when redistributing. Official source: https://github.com/ollama/ollama/blob/main/LICENSE
- Ollama native libraries/GPU components: preserve the distribution's included notices and comply with their individual redistribution terms. The provisioning script copies the complete library directory.
- Qwen3-4B model: Apache-2.0. The model's license is retained in the Ollama model license blob. Official source: https://huggingface.co/Qwen/Qwen3-4B/blob/main/LICENSE

Provisioning is a one-time installation/build step. Binary runtime/model assets are deliberately not committed to Git. A source clone needs provisioning; a prepared release includes those assets and runs local inference without downloading anything. Public redistribution requires reviewing the actual supplied licenses, especially GPU libraries; a successful local build is not a legal redistribution audit.
