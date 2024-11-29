using System.Text;
using Dapper;
using Npgsql;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{

public class RAGProcessor
{
    private readonly HttpClient _httpClient;
    private readonly string _dbConnectionString;
    string endpoint = "http://192.168.212.78:1234/v1/embeddings"; 
    public RAGProcessor(string dbConnectionString, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _dbConnectionString = dbConnectionString;
    }

   public async Task<string> ProcessQuery(string userQuery, List<string>? context = null)
{
    // Step 1: Estrarre parole chiave
    var keywords = await ExtractKeywordsFromQuery(userQuery);

    // Step 2: Generare embedding per le parole chiave
    var queryEmbedding = await GenerateEmbedding(keywords);

    // Step 3: Recuperare i dati rilevanti dal database
    var relevantData = await GetRelevantDataFromDatabase(queryEmbedding);

    // Step 4: Costruire il prompt con contesto
    var prompt = BuildPrompt(userQuery, relevantData, context);

    // Step 5: Inviare il prompt all'LLM
    var response = await SendPromptToLLM(prompt);

    return response;
}

private async Task<string> ExtractKeywordsFromQuery(string userQuery)
    {
        // prompt per estrarre le parole chiave
        string keywordPrompt = $"Extract only the keywords from the following query. Respond only with keywords, nothing else.\n\nUser Query: {userQuery}";

        HttpClient client2 = new HttpClient(new HttpHandler2());

        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("fake", "sium", httpClient: client2)
            .Build();

        string responseText = string.Empty;
        var responseStream = kernel.InvokePromptStreamingAsync(keywordPrompt);

        await foreach (var message in responseStream)
        {
            responseText += message;
        }

        return responseText.Trim(); 
    }


    private async Task<float[]> GenerateEmbedding(string inputText)
{
    // contenuto della request
    var requestBody = new { input = inputText };
    string jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
    var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
    
        try
        {
            // POST request a LMstudio
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, requestContent);

            // leggere e pars la risposta
            string responseBody = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(responseBody);
            var embeddingData = jsonObject["data"]?[0]?["embedding"];
            var embedding = embeddingData?.ToObject<List<float>>();

            return embedding?.ToArray() ?? throw new Exception("Failed to retrieve embedding.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to LM Studio: {ex.Message}");
            throw;
        }
    
}
    private async Task<List<(string Name, string Description)>> GetRelevantDataFromDatabase(float[] queryEmbedding)
    {
        using (var connection = new NpgsqlConnection(_dbConnectionString))
        {
            await connection.OpenAsync();

            // Query per trovare x embedding simili
            string sql = @"
                SELECT names, description
                FROM items
                ORDER BY embedding <-> @queryEmbedding::vector 
                LIMIT 2;";

            // ricavo nome e dati importanti usando dapper
            var relevantData = (await connection.QueryAsync<(string, string)>(sql, new { queryEmbedding })).AsList();
            return relevantData;
        }
    }

   private string BuildPrompt(string userQuery, List<(string Name, string Description)> relevantData, List<string>? context = null)
{
    var promptBuilder = new StringBuilder();

    // Includi eventuale contesto
    if (context != null && context.Count > 0)
    {
        promptBuilder.AppendLine("Chat History:");
        foreach (var message in context)
        {
            promptBuilder.AppendLine(message);
        }
        promptBuilder.AppendLine();
    }

    // Aggiungi istruzioni e query
    promptBuilder.AppendLine("Please do not ask additional questions. Base your response only on the selected data below, and respond as if you were a virtual assistant for a Adidas store, like a employee.");
    promptBuilder.AppendLine("User Query: " + userQuery);
    promptBuilder.AppendLine("Relevant Data:");

    foreach (var (name, description) in relevantData)
    {
        promptBuilder.AppendLine($"- Name: {name}");
        promptBuilder.AppendLine($"  Description: {description}");
    }

    return promptBuilder.ToString();
}

   private async Task<string> SendPromptToLLM(string prompt)
{
    // inizializzo httpCLient
    HttpClient client2 = new HttpClient(new HttpHandler2());

    // set up semantic kernel 
    var kernel = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion("fake", "sium", httpClient: client2)
        .Build();

    // inizializzo variabile che conterra il prompt
    string responseText = string.Empty;

    // Stream la risposta del prompt e mettiamo ogni riposta su responsetext
    var responseStream = kernel.InvokePromptStreamingAsync(prompt);
    await foreach (var message in responseStream)
    {
        responseText += message;
    }

    
    return responseText;
}
}

// deserializzo
public class EmbeddingResponse
{
    public float[]? Embedding { get; set; }
}

}