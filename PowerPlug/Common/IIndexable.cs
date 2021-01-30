namespace PowerPlug.Common
{
    /// <summary>
    /// Specifies an indexer with one dimension.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexable<in TKey, TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        TVal this[TKey key] { get; set; }
    }

    /// <summary>
    /// Specifies a read-only indexer with one dimension.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexableReadOnly<in TKey, out TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        TVal this[TKey key] { get; }
    }

    /// <summary>
    /// Specifies an indexer with two dimensions.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexableDouble<in TKey, TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        TVal this[TKey key, TKey key2] { get; set; }
    }

    /// <summary>
    /// Specifies a read-only indexer with two dimensions.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexableDoubleReadOnly<in TKey, out TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        TVal this[TKey key, TKey key2] { get; }
    }

    /// <summary>
    /// Specifies an indexer with three dimensions.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexableTriple<in TKey, TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="key2"></param>
        /// <param name="key3"></param>
        TVal this[TKey key, TKey key2, TKey key3] { get; set; }
    }

    /// <summary>
    /// Specifies a read-only indexer with three dimensions.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexableTripleReadOnly<in TKey, out TVal>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="key2"></param>
        /// <param name="key3"></param>
        /// <returns></returns>
        TVal this[TKey key, TKey key2, TKey key3] { get; }
    }
}
