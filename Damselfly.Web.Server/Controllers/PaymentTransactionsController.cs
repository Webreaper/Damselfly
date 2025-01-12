using Damselfly.Core.Constants;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [Authorize(Policy = PolicyDefinitions.s_IsAdmin)]
    [ApiController]
    [Route("/api/[controller]")]
    public class PaymentTransactionsController(PaymentTransactionService paymentTransactionService) : Controller
    {
        private readonly PaymentTransactionService _paymentTransactionService = paymentTransactionService;

        [HttpGet]
        [Route("GetByPhotoShootId/{photoShootId}")]
        public async Task<IActionResult> GetByPhotoShootId(Guid photoShootId) 
        { 
            var transactions = await _paymentTransactionService.GetPaymentTransactionsForPhotoShoot(photoShootId);
            return Ok(transactions);
        }
    }
}
