
using System.Collections.Generic;

namespace Damselfly.Web.Components;

public static class MudNoAutofill
{
    public static Dictionary<string, object> noAutoFillAttr = new Dictionary<string, object> { { "autocomplete", "new-password" } };
}
