using System;
using System.Collections.Generic;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// View Wrapper for a folder being displayed in the list-view
    /// item. We create/load this once and it provides bindable
    /// propeties that will simplify the view code. 
    /// </summary>
	public class FolderMetadata
	{
        public string DisplayName { get; set; }
		public int ImageCount { get; set; }
        public int ChildImageCount { get; set; }
		public DateTime? MaxImageDate { get; set; }
        public int Depth { get; set; } = 1;

        public int TotalImages => ImageCount + ChildImageCount;

        public override string ToString()
        {
            return $"{DisplayName} [{ImageCount} images, Date: {MaxImageDate}]";
        }
    }
}
