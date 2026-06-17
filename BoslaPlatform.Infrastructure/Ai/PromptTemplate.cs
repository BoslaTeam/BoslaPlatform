namespace BoslaPlatform.Infrastructure.AI;

public static class PromptTemplate
{
    public static string Build(string context, string question)
    {
        return $"You are an assistant. Use the context below to answer the question. If answer not present, respond with 'I don't know'.\n\nCONTEXT:\n{context}\n\nQUESTION:\n{question}";
    }
}
