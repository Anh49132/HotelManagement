using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int InvoiceId { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Đã thanh toán")]
        public decimal PaidAmount { get; set; }

        [Display(Name = "Còn lại")]
        public decimal RemainingAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0.")]
        [Display(Name = "Số tiền thanh toán")]
        public decimal PaymentAmount { get; set; }
    }
}