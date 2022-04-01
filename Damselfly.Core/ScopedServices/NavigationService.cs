using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public enum NavigationContexts
{
    Basket,
    Search
}

/// <summary>
/// The Navigation Context is used to perform next/prev operations, which allows a user
/// to move forward and backwards through a collection of images - either using the mouse
/// and icon, or via the keyboard. The context is set based on the focus, depending on how
/// the current image was loaded. So if the user double-clicks a basket image, then
/// forward/back navigates through the images in the basket. If they double-click an
/// image in the main browser list, we iterate through those images instead.
/// </summary>
public class NavigationService
{
    private Image theImage;

    public NavigationContexts Context { get; set; } = NavigationContexts.Search;
    public Image CurrentImage { get { return theImage; } set { theImage = value; NotifyStateChanged( theImage ); } }
    private readonly BasketService _basketService;
    private readonly SearchService _searchService;

    public NavigationService( BasketService basketService, SearchService searchService)
    {
        _basketService = basketService;
        _searchService = searchService;
    }

    private void NotifyStateChanged( Image newImage )
    {
        OnChange?.Invoke( newImage );
    }

    public event Action<Image> OnChange;

    /// <summary>
    /// Get the URL of the next or previous image
    /// </summary>
    /// <param name="image"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task<Image> GetNextImageAsync( bool next)
    {
        var task = Task.Run(() => GetNextImage( next ));
        return await task;
    }

    /// <summary>
    /// Calculates the URL of the next or previous image, based on the
    /// current context, and the current image position. This is passed
    /// back to the UI and is used to form the 'click' URL for the next
    /// or prev icons. 
    /// TODO: If Context = Search, then there may be more images
    /// that match the search. So call LoadMore here on wrap-around
    /// Otherwise we loop through 20 images, despite 200 matching the
    /// search.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    private Image GetNextImage( bool next )
    { 
        List<Image> navigationItems = null;

        if (Context == NavigationContexts.Basket)
            navigationItems = _basketService.BasketImages;
        else if (Context == NavigationContexts.Search)
            navigationItems = _searchService.SearchResults;

        if ( this.CurrentImage != null && navigationItems != null )
        {
            int currentIndex = navigationItems.FindIndex( x => x.ImageId == CurrentImage.ImageId );

            if (currentIndex != -1)
            {
                if (next)
                    currentIndex++;
                else
                    currentIndex--;

                if (currentIndex < 0)
                    currentIndex = navigationItems.Count - 1;
                else
                    currentIndex = currentIndex % navigationItems.Count;

                return navigationItems[currentIndex];
            }
        }

        return null;
    }
}
