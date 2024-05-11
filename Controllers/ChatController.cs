using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Azure.Core;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Azure;
using System.Reflection.PortableExecutable;

namespace pdfdatareader.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly string _endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        private readonly string _key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        private readonly string _model = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL");

        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetResponse(string userMessage)
        {
            OpenAIClient client = new OpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_key));
            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Messages = {
                    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
                    new ChatMessage(ChatRole.User, "Does Azure support GPT-4 ?"),
                    new ChatMessage(ChatRole.Assistant, "Yes, it does"),
                    new ChatMessage(ChatRole.User, userMessage)
                },
                MaxTokens = 1000
            };

            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(deploymentOrModelName: _model, chatCompletionOptions);
            var botResponse = response.Value.Choices.First().Message.Content;
            return Json(new { response = botResponse });
        }

        [HttpPost]
        public async Task<IActionResult> GetResponseFromPdf(string userMessage)
        {
            OpenAIClient client = new OpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_key));
            string pdfText = GetText("Data/azure.pdf");
            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Messages = {
                    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
                    new ChatMessage(ChatRole.User, "The following information is from the PDF text: " + pdfText),
                    new ChatMessage(ChatRole.User, userMessage)
                },
                MaxTokens = 1000,
                Temperature = 0
            };

            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(deploymentOrModelName: _model, chatCompletionOptions);
            var botResponse = response.Value.Choices.First().Message.Content;
            return Json(new { response = botResponse });
        }

        private static string GetText(string pdfFilePath)
        {
            using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(pdfFilePath)))
            {
                StringBuilder text = new StringBuilder();

                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    PdfPage pdfPage = pdfDoc.GetPage(page);
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfPage, strategy);
                    text.Append(currentText);
                }

                return text.ToString();
            }
        }
    }
}

