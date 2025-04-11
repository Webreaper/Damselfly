# Albums Pagination

This document describes the new pagination feature for albums in the Damselfly API.

## Overview

The albums pagination feature allows you to retrieve albums in smaller, manageable chunks instead of loading all albums at once. This is particularly useful when dealing with large numbers of albums.

## API Endpoint

### Paginated Albums

**Endpoint:** `GET /albums/paginated`

**Authorization:** Requires Firebase Admin policy

**Query Parameters:**

-   `PageIndex` (int, default: 1): Zero-based page index
-   `PageSize` (int, default: 10): Number of albums per page (max: 100)

**Example Request:**

```
GET /albums/paginated?PageIndex=0&PageSize=20
```

## Response Format

The endpoint returns a `PaginationResultModel<AlbumModel>` with the following structure:

```json
{
    "results": [
        {
            "albumId": "guid",
            "name": "Album Name",
            "urlName": "album-url-name",
            "description": "Album description",
            "isPublic": true,
            "password": null,
            "coverImageId": "guid",
            "isLocked": false,
            "images": []
        }
    ],
    "pageIndex": 0,
    "pageSize": 20,
    "pageCount": 5,
    "totalCount": 100
}
```

## Response Properties

-   `results`: Array of album models for the current page
-   `pageIndex`: Current page index (zero-based)
-   `pageSize`: Number of items per page
-   `pageCount`: Total number of pages
-   `totalCount`: Total number of albums

## Usage Examples

### Get first page with 10 albums

```
GET /albums/paginated?PageIndex=0&PageSize=10
```

### Get second page with 20 albums

```
GET /albums/paginated?PageIndex=1&PageSize=20
```

### Get all albums in chunks of 50

```
GET /albums/paginated?PageIndex=0&PageSize=50
GET /albums/paginated?PageIndex=1&PageSize=50
GET /albums/paginated?PageIndex=2&PageSize=50
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

The albums are ordered by name (alphabetically) for consistent pagination results.

## Future Enhancements

The `AlbumsPaginationRequest` model can be extended to support additional filtering options such as:

-   Search term filtering
-   Public/private album filtering
-   Date range filtering
-   Sorting options
