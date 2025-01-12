using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class PaymentTransactionService(ILogger<PaymentTransactionService> logger,
        ImageContext imageContext,
        IMapper mapper,
        IAuthService authService)
    {
        private readonly ILogger<PaymentTransactionService> _logger = logger;
        private readonly ImageContext _imageContext = imageContext;
        private readonly IMapper _mapper = mapper;
        private readonly IAuthService _authService = authService;


        public async Task<List<PaymentTransactionModel>> GetPaymentTransactionsForPhotoShoot(Guid photoShootId)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to GetPaymentTransactionsForPhotoShoot");
                return null;
            }
            _logger.LogInformation("Getting payment transactions for user {useremail} for photo shoot {photoShootId}", currentUserEmail, photoShootId);

            var transactions = await _imageContext.PaymentTransactions.Where(x => x.PhotoShootId == photoShootId).ToListAsync();

            return transactions.Select(_mapper.Map<PaymentTransactionModel>).ToList();
        }
    }
}
