using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiceController(ILogger<DiceController> logger) : ControllerBase
    {
        private static Random Random = new Random();
        
        [HttpGet("roll")]
        public int Roll(string dice)
        {
            logger.DiceRollRequested(dice);
            var result = Random.Next(1, 7);
            logger.DiceRollResult(dice, result.ToString());
            return result;
        }
    }
}
