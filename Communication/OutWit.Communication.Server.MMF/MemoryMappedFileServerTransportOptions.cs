using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.MMF
{
    public class MemoryMappedFileServerTransportOptions : ModelBase
    {
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

        #region Properties

        public long Size { get; set; }
        
        public string? Name { get; set; }

        #endregion
    }
}
