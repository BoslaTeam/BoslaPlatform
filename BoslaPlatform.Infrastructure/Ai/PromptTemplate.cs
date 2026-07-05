namespace BoslaPlatform.Infrastructure.AI;

public static class PromptTemplate
{
    public static string Build(string context, string question)
    {
        return $"You are an assistant. Use the context below to answer the question. If the context does not contain a direct answer, say you don't know but provide any relevant information from the context and explain what additional information is needed to answer the question.\n\nCONTEXT:\n{context}\n\nQUESTION:\n{question}";
    }
}
