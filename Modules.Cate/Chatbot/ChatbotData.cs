using System.Collections.Generic;
using Core.Cate.Biz;
using Core.Cate.Models;
using TSFramework.Libs.Processors;

namespace Modules.Cate.Chatbot
{
    public interface IChatbotData
    {
        bool CanView(string subject, bool project);
        List<RM_DigitalSalesModel> DigitalSalesList(RM_DigitalSalesSearchModel filter, out int total);
        RM_DigitalSalesModel DigitalSalesDetail(int id, string subject);
    }

    public sealed class ChatbotData : IChatbotData
    {
        public bool CanView(string subject, bool project)
        {
            if (string.IsNullOrWhiteSpace(subject)) return false;
            return AppProcessor.Author.IsAllow(subject, "Cate", "DigitalSales", "View")
                || AppProcessor.Author.IsAllow(subject, "Cate", project ? "Project" : "RM_BusinessOpportunity", "View");
        }

        public List<RM_DigitalSalesModel> DigitalSalesList(RM_DigitalSalesSearchModel filter, out int total)
        {
            return new RM_DigitalSalesBiz().LoadList(out total, filter);
        }

        public RM_DigitalSalesModel DigitalSalesDetail(int id, string subject)
        {
            return new RM_DigitalSalesBiz().GetByID(id, subject);
        }
    }
}
