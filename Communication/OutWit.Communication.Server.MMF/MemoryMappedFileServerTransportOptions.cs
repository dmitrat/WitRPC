using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.MMF
{
    public class MemoryMappedFileServerTransportOptions : ModelBase, IServerOptions
    {
        #region Constants

        private const string TRANSPORT = "MemoryMappedFile";

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Name: {Name}, Size: {Size}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is MemoryMappedFileServerTransportOptions options))
                return false;

            return Name.Is(options.Name) && 
                   Size.Is(options.Size);
        }

        public override MemoryMappedFileServerTransportOptions Clone()
        {
            return new MemoryMappedFileServerTransportOptions
            {
                Name = Name,
                Size = Size,
            };
        }

        #endregion

        #region IServerOptions

        public string Transport { get; } = TRANSPORT;

        public Dictionary<string, string> Data =>
            new()
            {
                { $"{nameof(Name)}", $"{Name}" },
                { $"{nameof(Size)}", $"{Size}" },
            };

        #endregion

        #region Properties

        public long Size { get; set; }
        
        public string? Name { get; set; }

        #endregion
    }
}
