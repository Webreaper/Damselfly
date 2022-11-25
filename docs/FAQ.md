
# Damselfly Frequently Asked Questions

[Return to Readme](../README.md)

- [Damselfly Frequently Asked Questions](#damselfly-frequently-asked-questions)
  - [Background/FAQ](#backgroundfaq)
    - [Do I have to run it in Docker?](#do-i-have-to-run-it-in-docker)
    - [Why 'Damselfly'?](#why-damselfly)
    - [What is the Damselfly Architecture?](#what-is-the-damselfly-architecture)
    - [How do I set up the Wordpress Integration?](#how-do-i-set-up-the-wordpress-integration)

## Background/FAQ

Some common questions/answers.

### Do I have to run it in Docker?

No, you can run it standalone. For most releases I'll provide docker images along with zip/tar files for the server and 
Desktop apps, for MacOS, Windows and Linux.

### Why 'Damselfly'?

Etymology of the name: DAM-_sel_-fly - **D**igital **A**sset **M**anagement that flies.

### What is the Damselfly Architecture?

Damselfly is written using C#/.Net 7 and Blazor WebAssembly. The data model and DB access is using Entity Framework Core. Currently the server supports Sqlite, but a future enhancement may be to add support for PostGres, MySql or MariaDB.

### How do I set up the Wordpress Integration?

Damselfly allows direct uploads of photographs to the media library of a Wordpress Blog. To enable this feature, you must configure your Wordpress site to support JWT authentication. For more details see [JWT Authentication for WP REST API](https://wordpress.org/plugins/jwt-authentication-for-wp-rest-api/).

To enable this option you’ll need to edit your .htaccess file adding the following:

    RewriteEngine on
    RewriteCond %{HTTP:Authorization} ^(.*)
    RewriteRule ^(.*) - [E=HTTP_AUTHORIZATION:%1]
    SetEnvIf Authorization "(.*)" HTTP_AUTHORIZATION=$1
    
The JWT needs a secret key to sign the token this secret key must be unique and never revealed. To add the secret key edit your wp-config.php file and add a new constant called JWT_AUTH_SECRET_KEY

    define('JWT_AUTH_SECRET_KEY', 'your-top-secret-key');

To enable the CORs Support edit your wp-config.php file and add a new constant called JWT_AUTH_CORS_ENABLE

    define('JWT_AUTH_CORS_ENABLE', true);

You can use a string from [here](https://api.wordpress.org/secret-key/1.1/salt/).

Once you have the site configured:

1. Install the [Wordpress JWT Authentication for WP REST API](https://wordpress.org/plugins/jwt-authentication-for-wp-rest-api/) 
plugin.
2. Use the config page in Damselfly to set the website URL, username and password. I recommend setting up a dedicated user account 
for Damselfly to use.