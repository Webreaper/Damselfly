using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Utils
{
    public static class EmailContent
    {
        #region templates
        private static string _baseEmailTemplate = @"<!doctype html>
<html lang=""en"">
<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <title>{{title}}</title>
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
    <link href=""https://fonts.googleapis.com/css2?family=IM+Fell+English+SC&family=IM+Fell+English:ital@0;1&display=swap"" rel=""stylesheet"">
    <style media=""all"" type=""text/css"">

        @media all {
            .btn-primary table td:hover {
                box-shadow: grey 0px 2px 8px 0px !important;
              }

              .btn-primary a:hover {
                box-shadow: grey 0px 2px 8px 0px !important;
              }
        }

        @media only screen and (max-width: 640px) {
            .main p,
            .main td,
            .main span {
                font-size: 16px !important;
            }

            .wrapper {
                padding: 8px !important;
            }

            .content {
                padding: 0 !important;
            }

            .container {
                padding: 0 !important;
                padding-top: 8px !important;
                width: 100% !important;
            }

            .main {
                border-left-width: 0 !important;
                border-radius: 0 !important;
                border-right-width: 0 !important;
            }

            .btn table {
                max-width: 100% !important;
                width: 100% !important;
            }

            .btn a {
                font-size: 16px !important;
                max-width: 100% !important;
                width: 100% !important;
            }
        }

        @media all {
            .ExternalClass {
                width: 100%;
            }

                .ExternalClass,
                .ExternalClass p,
                .ExternalClass span,
                .ExternalClass font,
                .ExternalClass td,
                .ExternalClass div {
                    line-height: 100%;
                }

            .apple-link a {
                color: inherit !important;
                font-family: inherit !important;
                font-size: inherit !important;
                font-weight: inherit !important;
                line-height: inherit !important;
                text-decoration: none !important;
            }

            #MessageViewBody a {
                color: inherit;
                text-decoration: none;
                font-size: inherit;
                font-family: inherit;
                font-weight: inherit;
                line-height: inherit;
            }
        }
    </style>
</head>
<body style=""font-family: Helvetica, sans-serif; -webkit-font-smoothing: antialiased; font-size: 16px; line-height: 1.3; -ms-text-size-adjust: 100%; -webkit-text-size-adjust: 100%; background-color: #f4f5f6; margin: 0; padding: 0;"">
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""body"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f4f5f6; width: 100%;"" width=""100%"" bgcolor=""#f4f5f6"">
        <tr>
            <td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td>
            <td class=""container"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; max-width: 600px; padding: 0; padding-top: 24px; width: 600px; margin: 0 auto;"" width=""600"" valign=""top"">
                <div class=""content"" style=""box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;"">

                    <!-- START CENTERED WHITE CONTAINER -->
                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""main"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background: #ffffff; border: 1px solid #eaebed; border-radius: 16px; width: 100%;"" width=""100%"">

                        <!-- START MAIN CONTENT AREA -->
                        <tr>
                            <td class=""wrapper"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; box-sizing: border-box; padding: 24px;"" valign=""top"">
                                {{body}}
                            <p style=""font-size: small;"">Contact us at: <a href=""mailto:honeyandThymePhotography@gmail.com"">HoneyAndThymePhotography@gmail.com</a></p>
                            </td>
                        </tr>

                        <!-- END MAIN CONTENT AREA -->
                    </table>

                    <!-- START FOOTER -->
                    <div class=""footer"" style=""clear: both; padding-top: 24px; text-align: center; width: 100%;"">
                        <img src=""https://honeyandthymephotography.com/assets/assets/images/logo.png"" alt=""Honey+Thyme Logo"" style=""width: 100px; height: 100px;"">
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;"" width=""100%"">
                                <tr>
                                    <td class=""content-block"" style=""font-family: 'IM Fell English', serif; vertical-align: top; color: #9a9ea6; font-size: 16px; text-align: center;"" valign=""top"" align=""center"">
                                        <span class=""apple-link"" style=""color: #9a9ea6; font-size: 16px; text-align: center;"">Honey+Thyme Photography</span>

                                    </td>
                                </tr>
                            </table>
                        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" align=""center"" style=""margin-top: 16px;"">
                        <tr>
                          <td style=""padding-right: 8px;"">
                            <a href=""https://www.facebook.com/share/1GMseZYBMV/?mibextid=qi2Omg"">
                              <img src=""https://honeyandthymephotography.com/assets/assets/images/Facebook_Logo_Primary.png"" alt=""Facebook"" style=""width: 50px; height: 50px; border: 0; display: block;"">
                            </a>
                          </td>
                          <td>
                            <a href=""https://www.instagram.com/honeyandthymephotography?igsh=ZDd4cTk2M3cwYzU5"">
                              <img src=""https://honeyandthymephotography.com/assets/assets/images/Instagram_Glyph_Gradient.png"" alt=""Instagram"" style=""width: 50px; height: 50px; border: 0; display: block;"">
                            </a>
                          </td>
                        </tr>
                      </table>
                    </div>

                    <!-- END FOOTER -->
                    <!-- END CENTERED WHITE CONTAINER -->
                </div>
            </td>
            <td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td>
        </tr>
    </table>
</body>
</html>
";

        private static string _callToActionTemplate = @"<table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""btn btn-primary"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; box-sizing: border-box; width: 100%; min-width: 100%;"" width=""100%"">
    <tbody>
        <tr>
            <td align=""left"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; padding-bottom: 16px;"" valign=""top"">
                <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: auto;"">
                    <tbody>
                        <tr>
                            <td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; border-radius: 4px; text-align: center; background-color: #bb964e;"" valign=""top"" align=""center"" bgcolor=""#bb964e""> <a href=""{{url}}"" target=""_blank"" style=""border: solid 2px #bb964e; border-radius: 4px; box-sizing: border-box; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize; background-color: #bb964e; border-color: #bb964e; color: #ffffff;"">{{label}}</a> </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>";

        private static string _paragraphTemplate = @"<p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">{{paragraph}}</p>";
        private static string _tableTemplate = @"<table role=""presentation"" cellpadding=""5"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; box-sizing: border-box; width: 100%; min-width: 100%;"" width=""100%"">
    <tbody>
        {{rows}}
    </tbody>
</table>
";
        private static string _rowTemplate = @"<tr>
    {{cells}}
</tr>";
        private static string _cellTemplate = @"<td>{{data}}</td>";
        #endregion



        public static string FormatEmail(string title, string[] paragraphs, string? callToActionUrl = null, string? callToActionTitle = null, string[][]? tableElements = null)
        {
            var body = new StringBuilder();
            foreach(var paragraph in paragraphs)
            {
                var line = _paragraphTemplate.Replace("{{paragraph}}", paragraph);
                body.AppendLine(line);
            }
            if( tableElements != null )
            {
                var tableBuilder = new StringBuilder();

                foreach( var row in tableElements )
                {
                    var rowBuilder = new StringBuilder();
                    foreach( var cell in row )
                    {
                        var cellHtml = _cellTemplate.Replace("{{data}}", cell);
                        rowBuilder.AppendLine(cellHtml);
                    }
                    var rowHtml = _rowTemplate.Replace("{{cells}}", rowBuilder.ToString());
                    tableBuilder.AppendLine(rowHtml);
                }
                var tableHtml = _tableTemplate.Replace("{{rows}}", tableBuilder.ToString());
                body.AppendLine(tableHtml);
            }
            if (callToActionTitle != null && callToActionUrl != null) 
            {
                var callToAction = _callToActionTemplate.Replace("{{label}}", callToActionTitle)
                    .Replace("{{url}}", callToActionUrl);
                body.AppendLine(callToAction);
            }
            return _baseEmailTemplate.Replace("{{body}}", body.ToString())
                .Replace("{{title}}", title);
        }
    }
}
