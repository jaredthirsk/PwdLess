using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PwdLess.Models
{
    public interface IAdditionalUserInfo
    {
        string FavColor { get; set; }
    }
}
