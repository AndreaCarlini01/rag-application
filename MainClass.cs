namespace ConsoleApp1
{
 public class MainClass
{
   static async Task Main(string[] args)
    {
        // Inizializza HttpClient e RAGProcessor
        HttpClient client = new HttpClient(new HttpHandler2());
        string dbConnectionString = "Host=localhost;Username=postgres;Password=andreastage;Database=utente;Port=5431";
        var ragProcessor = new RAGProcessor(dbConnectionString, client);

        // Inizializza la chat session
        var chatSession = new ChatSession(ragProcessor);

        // Avvia il loop della chat
        Console.WriteLine("Welcome to the interactive chat! Type 'exit' to end the session.");
        await chatSession.StartChat();
    }
}
}