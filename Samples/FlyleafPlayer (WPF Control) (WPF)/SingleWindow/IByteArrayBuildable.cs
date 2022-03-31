using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityClasses
    {
    /// <summary>
    /// Allows classes to "self describe" into a ByteArrayBuilder
    /// </summary>
    /// <example>
    ///    public class Card : IByteArrayBuildable
    ///        {
    ///        public string Name { get; set; }
    ///        public DateTime DOB { get; set; }
    ///        void IByteArrayBuildable.Insert(ByteArrayBuilder bab)
    ///            {
    ///            bab.Append(Name);
    ///            bab.Append(DOB);
    ///            }
    ///        void IByteArrayBuildable.Extract(ByteArrayBuilder bab)
    ///            {
    ///            Name = bab.GetString();
    ///            DOB = bab.GetDateTime();
    ///            }
    ///        } 
    ///    public class Tester
    ///        {
    ///        public Tester()
    ///            {
    ///            Card card = new Card();
    ///            card.Name = "Joe Smith";
    ///            card.DOB = new DateTime(1999, 12, 31);
    ///            byte[] data;
    ///            using (ByteArrayBuilder babIn = new ByteArrayBuilder())
    ///                {
    ///                babIn.Append(card);
    ///                data = babIn.ToArray();
    ///                }
    ///            using (ByteArrayBuilder babOut = new ByteArrayBuilder(data))
    ///                {
    ///                babOut.GetBuildable(card);
    ///                }
    ///            }
    ///        }
    /// </example>
    public interface IByteArrayBuildable
        {
        /// <summary>
        /// Insert the implementing class into a ByteArrayBuilder
        /// </summary>
        /// <param name="bab">Data storage</param>
        void Insert(ByteArrayBuilder bab);
        /// <summary>
        /// Extract the implementing class from a ByteArrayBuilder
        /// </summary>
        /// <param name="bab">Data storage</param>
        void Extract(ByteArrayBuilder bab);
        }
    }