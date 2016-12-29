using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Auth.Data
{

    public interface ITotpRepository
    {

    }

    public class InMemoryTotpRepository : ITotpRepository
    {

    }

    public class RedisTotpRepository : ITotpRepository
    {
    }

}
