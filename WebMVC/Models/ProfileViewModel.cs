using System;
using System.ComponentModel.DataAnnotations;

namespace WebMVC.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Họ")]
        public string? FirstName { get; set; }

        [Display(Name = "Tên")]
        public string? LastName { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Giới tính")]
        [RegularExpression("^[MFO]$", ErrorMessage = "Giới tính phải là M (Nam), F (Nữ), hoặc O (Khác).")]
        public string? Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Nghề nghiệp")]
        public string? Occupation { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }
    }
}