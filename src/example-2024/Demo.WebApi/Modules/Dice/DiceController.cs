using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class DiceController(ILogger<DiceController> logger) : ControllerBase
    {
        private static Random Random = new();
        
        [HttpGet("roll")]
        public int Roll(string dice)
        {
            logger.DiceRollRequested(dice);
            
            var matches = SimpleDiceParse().Match(dice);
            var count = int.Parse(matches.Groups["count"].ToString());
            var size = int.Parse(matches.Groups["size"].ToString());

            var result = 0;
            for (var counter = 0; counter < count; counter++)
            {
                var roll = Random.Next(1, size + 1);
                result += roll;
            }

            logger.DiceRollResult(dice, result.ToString());
            return result;
        }
        
        [GeneratedRegex(
            @"^(?<count>\d*)[dD](?<size>\d+)$",
            RegexOptions.Compiled,
            1_000
        )]
        private static partial Regex SimpleDiceParse(); // groups of {digits}{nondigits}
    }
}
