using Microsoft.AspNetCore.Identity;
using PwdLess.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.ManageViewModels
{
    public class HistoryViewModel
    {
        public IList<AuthEvent> Events { get; set; }
    }
}
