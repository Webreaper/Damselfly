using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientBasketService : IUserBasketService, IBasketService
{
    private readonly IImageCacheService _imageCache;
    private readonly NotificationsService _notifications;
    private readonly IUserService _userService;
    private readonly RestClient httpClient;
    protected ILogger<ClientBasketService> _logger;

    public ClientBasketService(RestClient client, NotificationsService notifications,
        IImageCacheService imageCache,
        IUserService userService,
        ILogger<ClientBasketService> logger)
    {
        httpClient = client;
        _userService = userService;
        _imageCache = imageCache;
        _notifications = notifications;
        _logger = logger;

        _notifications.SubscribeToNotificationAsync<BasketChanged>(NotificationType.BasketChanged,
            HandleServerBasketChange);
    }

    public event Action<BasketChanged> OnBasketChanged;

    /// <summary>
    ///     The list of selected images in the basket
    /// </summary>
    public ICollection<Image> BasketImages => CurrentBasket == null
        ? new List<Image>()
        : CurrentBasket.BasketEntries.Select(x => x.Image).ToList();

    public Basket CurrentBasket { get; private set; }

    public async Task Clear(int basketId)
    {
        await httpClient.CustomPostAsync($"/api/basket/clear/{basketId}");
    }

    public async Task Delete(int basketId)
    {
        await httpClient.CustomDeleteAsync($"/api/basket/{basketId}");
    }

    public async Task<Basket> GetBasketById(int basketId)
    {
        var basket = await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basket/{basketId}");
        return basket;
    }

    public async Task<Basket> SwitchToBasket(int basketId)
    {
        try
        {
            var newBasket = await GetBasketById(basketId);

            await SetCurrentBasket(newBasket);

            return newBasket;
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Attempted to switch to unknown basket ID {basketId}");
            throw;
        }
    }

    public async Task<Basket> SwitchToDefaultBasket(int? userId)
    {
        Basket basket;

        if ( userId is null )
            basket = await httpClient.CustomGetFromJsonAsync<Basket>("/api/basketdefault");
        else
            basket = await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basketdefault/{userId}");

        await SetCurrentBasket(basket);

        return basket;
    }

    public async Task SetImageBasketState(int basketId, bool newState, ICollection<int> images)
    {
        var payload = new BasketStateRequest
        {
            BasketId = basketId,
            NewState = newState,
            ImageIds = images
        };
        await httpClient.CustomPostAsJsonAsync("/api/basketimage/state", payload);

        // We don't notify the state changed here - it'll be notified from the server
    }

    public async Task<Basket> Create(string name)
    {
        return await Create(name, _userService.UserId);
    }

    public async Task<Basket> Create(string name, int? userId)
    {
        var payload = new BasketCreateRequest { Name = name, UserId = userId };
        return await httpClient.CustomPostAsJsonAsync<BasketCreateRequest, Basket>("/api/basket", payload);
    }

    public async Task Save(Basket basket)
    {
        var response = await httpClient.CustomPutAsJsonAsync("/api/basket", basket);
    }

    public async Task<Basket> GetDefaultBasket(int? userId)
    {
        return await httpClient.CustomGetFromJsonAsync<Basket>($"/api/baskets/{_userService.UserId}");
    }

    public async Task<ICollection<Basket>> GetUserBaskets()
    {
        return await GetUserBaskets(_userService.UserId);
    }

    public async Task<ICollection<Basket>> GetUserBaskets(int? userId)
    {
        return await httpClient.CustomGetFromJsonAsync<ICollection<Basket>>($"/api/baskets/{userId}");
    }

    public async Task<int> CopyImages(int sourceBasketId, int destBasketId)
    {
        return await httpClient.CustomGetFromJsonAsync<int>($"/api/basket/copy/{sourceBasketId}/{destBasketId}");
    }

    public async Task<int> CopyImages(int destBasketId)
    {
        return await CopyImages(CurrentBasket.BasketId, destBasketId);
    }

    public async Task Clear()
    {
        await Clear(CurrentBasket.BasketId);
    }

    public async Task SetImageBasketState(bool newState, ICollection<int> imageIds)
    {
        await SetImageBasketState(CurrentBasket.BasketId, newState, imageIds);
    }

    public bool IsSelected(Image image)
    {
        return BasketImages.Any(x => x.ImageId == image.ImageId);
    }

    public async Task<Basket> SwitchToDefaultBasket()
    {
        return await SwitchToDefaultBasket(_userService.UserId);
    }

    private async Task HandleServerBasketChange(BasketChanged change)
    {
        if ( CurrentBasket.BasketId == change.BasketId )
        {
            Basket newBasket;

            if ( change.ChangeType == BasketChangeType.BasketDeleted )
                newBasket = await GetDefaultBasket(_userService.UserId);
            else
                newBasket = await GetBasketById(change.BasketId);

            await SetCurrentBasket(newBasket);
        }
    }

    public async Task SetCurrentBasket(Basket newBasket)
    {
        CurrentBasket = newBasket;

        // See if there's any images that need loading
        var imagesToLoad = CurrentBasket.BasketEntries
            .Where(x => x.Image == null)
            .Select(x => x.ImageId)
            .ToList();

        if ( imagesToLoad.Any() )
        {
            // Load the basket images into the cache...
            var images = _imageCache.GetCachedImages(imagesToLoad);

            // ...and Attach them to the basket entries
            foreach ( var be in CurrentBasket.BasketEntries ) be.Image = await _imageCache.GetCachedImage(be.ImageId);
        }

        var change = new BasketChanged { ChangeType = BasketChangeType.BasketChanged, BasketId = newBasket.BasketId };
        OnBasketChanged?.Invoke(change);
    }

    public async Task<Basket> GetDefaultBasket()
    {
        return await GetDefaultBasket(_userService.UserId);
    }
}