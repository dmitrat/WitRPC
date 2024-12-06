using System;
using OutWit.Common.MessagePack;

namespace OutWit.Communication.Converters
{
    public sealed class ValueConverterMessagePack : ValueConverterBase
    {
        protected override bool TryUnpack(object origValue, Type destType, out object? destValue)
        {
            try
            {
                destValue = null;

                if (origValue is not byte[] bytes)
                    return false;

                destValue = bytes.FromPackBytes(destType);

                return destValue != null;
            }
            catch
            {
                destValue = null;
                return false;
            }
        }

        protected override bool TryRestore(object origValue, Type destType, out object? destValue)
        {
            try
            {
                var bytes = origValue.ToPackBytes();
                destValue = bytes.FromPackBytes(destType);
                return true;
            }
            catch
            {
                destValue = null;
                return false;
            }
        }
    }
}
