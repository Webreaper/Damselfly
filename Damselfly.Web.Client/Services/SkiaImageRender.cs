using System.Net;

namespace Damselfly.Web.Client.Services;

public static class SkiaImageRender
{
    public static byte[] LoadImageFromURL( this string url )
    {
        HttpWebResponse response = null;
        try
        {
            var request = (HttpWebRequest)WebRequest.Create( url );
            request.Method = "HEAD";
            request.Timeout = 2000; // miliseconds

            response = (HttpWebResponse)request.GetResponse();

            if( response.StatusCode == HttpStatusCode.OK ) //Make sure the URL is not empty and the image is there
            {
                // download the bytes
                byte[] stream = null;
                using( var webClient = new WebClient() )
                {
                    stream = webClient.DownloadData( url );
                    return stream;
                }
            }
        }
        catch( Exception ex )
        {
            Console.WriteLine( $"Exception: {ex}" );
        }
        finally
        {
            // Don't forget to close your response.
            if( response != null ) response.Close();
        }

        return null;
    }
}