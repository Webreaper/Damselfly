# Photo Shoot Pagination

This document describes the new pagination feature for photo shoots in the Damselfly API.

## Overview

The photo shoot pagination feature allows you to retrieve photo shoots in smaller, manageable chunks instead of loading all photo shoots at once. This is particularly useful when dealing with large numbers of photo shoots and provides better performance for filtering and searching.

## API Endpoint

### Paginated Photo Shoots

**Endpoint:** `GET /api/photoshoot/paginated`

**Authorization:** Requires Firebase Admin policy

**Query Parameters:**

-   `PageIndex` (int, default: 1): Zero-based page index
-   `PageSize` (int, default: 10): Number of photo shoots per page (max: 100)
-   `StartDate` (DateTime?, optional): Filter photo shoots from this date (inclusive)
-   `EndDate` (DateTime?, optional): Filter photo shoots before this date (exclusive)
-   `BookStatuses` (List<PhotoShootStatusEnum>?, optional): Filter by specific statuses
-   `PhotoShootType` (PhotoShootTypeEnum?, optional): Filter by photo shoot type

**Example Request:**

```
GET /api/photoshoot/paginated?PageIndex=0&PageSize=20&StartDate=2024-01-01&EndDate=2024-12-31
```

## Response Format

The endpoint returns a `PaginationResultModel<PhotoShootModel>` with the following structure:

```json
{
    "results": [
        {
            "photoShootId": "guid",
            "responsiblePartyName": "John Doe",
            "responsiblePartyEmailAddress": "john@example.com",
            "nameOfShoot": "Wedding Photos",
            "description": "Beautiful wedding ceremony",
            "dateTimeUtc": "2024-06-15T14:00:00Z",
            "endDateTimeUtc": "2024-06-15T16:00:00Z",
            "location": "Central Park",
            "price": 500.0,
            "deposit": 100.0,
            "discount": 0.0,
            "discountName": null,
            "paymentRemaining": 400.0,
            "status": 2,
            "photoShootType": 0,
            "albumId": "guid"
        }
    ],
    "pageIndex": 0,
    "pageSize": 20,
    "pageCount": 5,
    "totalCount": 100
}
```

## Response Properties

-   `results`: Array of photo shoot models for the current page
-   `pageIndex`: Current page index (zero-based)
-   `pageSize`: Number of items per page
-   `pageCount`: Total number of pages
-   `totalCount`: Total number of photo shoots matching the filters

## Photo Shoot Status Values

-   `0`: Unbooked
-   `1`: Booked
-   `2`: Confirmed
-   `3`: Paid
-   `4`: Delivered
-   `5`: Cancelled
-   `6`: Deleted

## Photo Shoot Type Values

-   `0`: CustomBooking
-   `1`: CalendarBooking

## Usage Examples

### Get first page with 10 photo shoots

```
GET /api/photoshoot/paginated?PageIndex=0&PageSize=10
```

### Get second page with 20 photo shoots

```
GET /api/photoshoot/paginated?PageIndex=1&PageSize=20
```

### Get confirmed photo shoots in date range

```
GET /api/photoshoot/paginated?PageIndex=0&PageSize=50&StartDate=2024-01-01&EndDate=2024-12-31&BookStatuses=2
```

### Get calendar bookings only

```
GET /api/photoshoot/paginated?PageIndex=0&PageSize=25&PhotoShootType=1
```

### Get upcoming photo shoots (next 30 days)

```
GET /api/photoshoot/paginated?PageIndex=0&PageSize=50&StartDate=2024-01-01&EndDate=2024-02-01&BookStatuses=1,2,3
```

## Error Handling

-   **400 Bad Request**: If `PageIndex` is less than 0 or `PageSize` is not between 1 and 100
-   **401 Unauthorized**: If user is not authenticated or doesn't have admin privileges
-   **200 OK**: Successful response with paginated data

## Implementation Details

The pagination feature uses the existing `Pagination.PaginateQuery` utility method which:

1. Takes an `IOrderedQueryable<T>` as input
2. Applies `Skip` and `Take` operations for pagination
3. Maps the results using AutoMapper
4. Returns a `PaginationResultModel<T>` with metadata

The photo shoots are ordered by `DateTimeUtc` (chronologically) for consistent pagination results.

## Filtering Options

The pagination request supports all the same filtering options as the original `PhotoShootFilerRequest`:

-   **Date Range**: Filter photo shoots within a specific date range
-   **Status Filtering**: Filter by one or more photo shoot statuses
-   **Type Filtering**: Filter by photo shoot type (CustomBooking or CalendarBooking)

## Performance Benefits

-   **Reduced Memory Usage**: Only loads the requested page of photo shoots
-   **Faster Response Times**: Smaller data transfers
-   **Better User Experience**: Faster page loads and smoother scrolling
-   **Scalable**: Handles large numbers of photo shoots efficiently

## Future Enhancements

The `PhotoShootPaginationRequest` model can be extended to support additional filtering options such as:

-   Search term filtering (by name, description, location)
-   Price range filtering
-   Location-based filtering
-   Payment status filtering
-   Sorting options (by date, price, status, etc.)
