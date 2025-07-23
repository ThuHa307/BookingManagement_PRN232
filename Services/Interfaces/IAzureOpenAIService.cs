using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Dtos.ChatBot;

namespace Services.Interfaces
{
    public interface IAzureOpenAIService
    {
        Task<string> GenerateDataPost(PostDataAIDto model);
        Task<string> ChatWithAIAsync(string userMessage);
    }
}