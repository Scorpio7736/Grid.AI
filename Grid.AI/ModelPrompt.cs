namespace Grid.AI;

public static class ModelPrompt
{
    public static string Render(ModelRequest request)
    {
        if (request.PromptTemplate == "raw")
        {
            if (request.SystemInstruction is not null || request.PromptVersion is not null) throw new ArgumentException("System instructions require an explicit supported template.");
            return request.Prompt;
        }
        if (request.PromptTemplate != "qwen-chat-v1" || string.IsNullOrWhiteSpace(request.SystemInstruction) ||
            request.SystemInstruction.Length > 16384 || string.IsNullOrWhiteSpace(request.PromptVersion) ||
            request.PromptVersion.Length > 128 || request.PromptVersion.Any(char.IsControl)) throw new ArgumentException("Invalid versioned prompt.");
        static string Data(string text) => text.Replace("<|", "[|").Replace("|>", "|]");
        var framed = "<|im_start|>system\n" + Data(request.SystemInstruction) + "<|im_end|>\n<|im_start|>user\n" +
            Data(request.Prompt) + "<|im_end|>\n<|im_start|>assistant\n<think>\n\n</think>\n\n";
        if (framed.Length > 65536) throw new ArgumentException("Framed prompt exceeds budget.");
        return framed;
    }
}
