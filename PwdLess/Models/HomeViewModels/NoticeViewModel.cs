using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.HomeViewModels
{
    public class NoticeViewModel
    {
        public NoticeType NoticeType { get; set; }

        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string Title { get; set; } = "An unexpected error occured.";

        [Display(Name = "Description")]
        public string Description { get; set; } = "Please try again later.";

        public bool ShowBackButton { get; set; } = false;
    }
}