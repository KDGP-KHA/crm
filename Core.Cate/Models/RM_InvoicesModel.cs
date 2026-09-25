using System.Collections.Generic;
using System.Web;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_InvoicesModel : BaseModel
    {
        public string InvoiceID { get; set; }
        public int ContractID { get; set; }
        //Serial 

        [CustomRequired]
        [CustomDisplayName("Serial_Label")]
        public string Serial { get; set; }
        //Number 
        [CustomRequired]
        [CustomDisplayName("Number_Label")]
        public string Number { get; set; }
        //Buyer 
        [CustomRequired]
        [CustomDisplayName("Buyer_Label")]
        public string Buyer { get; set; }
        //TaxCode 
        [CustomRequired]
        [CustomDisplayName("TaxCode_Label")]
        public string TaxCode { get; set; }
        //VAT_Rate 
        [CustomRequired]
        [CustomDisplayName("VAT_Rate_Label")]
        public int VAT_Rate { get; set; } = 10;
        //TotalAmount 
        [CustomDisplayName("TotalAmount_Label")]
        public double TotalAmount { get; set; }
        //VAT_Amount 
        [CustomDisplayName("VAT_Amount_Label")]
        public double VAT_Amount { get; set; }
        //TotalPayment 
        [CustomRequired]
        [CustomDisplayName("TotalPayment_Label")]
        public double TotalPayment { get; set; }
        public List<HttpPostedFileBase> Files { get; set; }
        public List<RM_InvoicesFilePathModel> FilePaths { get; set; }
    }
}
