using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class ChatSession
    {
        private readonly RAGProcessor _ragProcessor;
    private readonly List<string> _chatHistory;

    public ChatSession(RAGProcessor ragProcessor)
    {
        _ragProcessor = ragProcessor;
        _chatHistory = new List<string>();
    }

    public async Task StartChat()
{
    while (true)
    {
        Console.WriteLine("\nYou: ");
        string userInput = Console.ReadLine();

        if (userInput?.Trim().ToLower() == "exit")
        {
            Console.WriteLine("Ending the chat. goodbye");
            break;
        }

        // Aggiungi l'input utente al contesto
        _chatHistory.Add($"You: {userInput}");

        // Passa il contesto al RAGProcessor
        string response = await _ragProcessor.ProcessQuery(userInput, _chatHistory);

        // Aggiungi la risposta dell'LLM al contesto
        _chatHistory.Add($"LLM: {response}");

        // Mostra la risposta
        Console.WriteLine("\nLLM: ");
        Console.WriteLine(response);
    }
}
    }
}