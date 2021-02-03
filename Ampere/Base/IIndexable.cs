namespace Ampere.Base
{
    /// <summary>
    /// Specifies an indexer with one dimension.
    /// </summary>
    /// <typeparam name="TKey">The element type of the key</typeparam>
    /// <typeparam name="TVal">The element type of the value</typeparam>
    public interface IIndexable<in TKey, TVal>
    {
        /// <summary>
        /// The one dimension read/write indexer.
        /// </summary>
        /// <param name="key">The key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
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
        /// The one dimension readonly indexer.
        /// </summary>
        /// <param name="key">The key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
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
        /// The two dimension read/write indexer.
        /// </summary>
        /// <param name="key">The first key to assign to the indexer</param>
        /// <param name="key2">The second key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
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
        /// The two dimension readonly indexer.
        /// </summary>
        /// <param name="key">The first key to assign to the indexer</param>
        /// <param name="key2">The second key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
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
        /// The two dimension read/write indexer.
        /// </summary>
        /// <param name="key">The first key to assign to the indexer</param>
        /// <param name="key2">The second key to assign to the indexer</param>
        /// <param name="key3">The third key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
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
        /// The third dimension readonly indexer.
        /// </summary>
        /// <param name="key">The first key to assign to the indexer</param>
        /// <param name="key2">The second key to assign to the indexer</param>
        /// <param name="key3">The third key to assign to the indexer</param>
        /// <returns>The TVal generic type</returns>
        TVal this[TKey key, TKey key2, TKey key3] { get; }
    }
}
