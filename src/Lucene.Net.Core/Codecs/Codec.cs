using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Encodes/decodes an inverted index segment.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) is
    /// written into the index. In order for the segment to be read, the
    /// name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// This method uses <see cref="ICodecFactory.GetCodec(string)"/> to resolve codec names.
    /// <para/>
    /// To implement your own codec:
    /// <list type="number">
    ///     <item>Subclass this class.</item>
    ///     <item>Subclass <see cref="DefaultCodecFactory"/> and add the line 
    ///         <c>base.ScanForCodecs(typeof(YourCodec).GetTypeInfo().Assembly)</c> 
    ///         to the constructor. If you have any codec classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="ExcludeCodecFromScanAttribute"/> 
    ///         to them so they are ignored by the scan.</item>
    ///     <item>set the new <see cref="ICodecFactory"/> by calling <see cref="SetCodecFactory"/> at application startup.</item>
    /// </list>
    /// If your codec has dependencies, you may also override <see cref="DefaultCodecFactory.GetCodec(Type)"/> to inject 
    /// them via pure DI or a DI container. See <a href="http://blog.ploeh.dk/2014/05/19/di-friendly-framework/">DI-Friendly Framework</a>
    /// to understand the approach used.
    /// <para/>
    /// <b>Codec Names</b>
    /// <para/>
    /// Unlike the Java version, codec names are by default convention-based on the class name. 
    /// If you name your custom codec class "MyCustomCodec", the codec name will the same name 
    /// without the "Codec" suffix: "MyCustom".
    /// <para/>
    /// You can override this default behavior by using the <see cref="CodecNameAttribute"/> to
    /// name the codec differently than this convention. Codec names must be all ASCII alphanumeric, 
    /// and less than 128 characters in length.
    /// </summary>
    /// <seealso cref="DefaultCodecFactory"/>
    /// <seealso cref="ICodecFactory"/>
    /// <seealso cref="CodecNameAttribute"/>
    public abstract class Codec //: NamedSPILoader.INamedSPI
    {
        private static ICodecFactory codecFactory;
        private readonly string name;

        static Codec()
        {
            codecFactory = new DefaultCodecFactory();
            defaultCodec = Codec.ForName("Lucene46");
        }

        /// <summary>
        /// Sets the <see cref="ICodecFactory"/> instance used to instantiate
        /// <see cref="Codec"/> subclasses.
        /// </summary>
        /// <param name="codecFactory">The new <see cref="ICodecFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="codecFactory"/> parameter is <c>null</c>.</exception>
        public static void SetCodecFactory(ICodecFactory codecFactory)
        {
            if (codecFactory == null)
                throw new ArgumentNullException("codecFactory");
            Codec.codecFactory = codecFactory;
            defaultCodec = Codec.ForName("Lucene46");
        }

        /// <summary>
        /// Gets the associated codec factory.
        /// </summary>
        /// <returns>The codec factory.</returns>
        public static ICodecFactory GetCodecFactory()
        {
            return codecFactory;
        }

        /// <summary>
        /// Creates a new codec.
        /// <para/>
        /// The provided name will be written into the index segment: in order for
        /// the segment to be read this class should be registered by subclassing <see cref="DefaultCodecFactory"/> and
        /// calling <see cref="DefaultCodecFactory.ScanForCodecs(System.Reflection.Assembly)"/> in the class constructor. 
        /// The new <see cref="ICodecFactory"/> can be registered by calling <see cref="SetCodecFactory"/> at application startup.</summary>
        protected Codec()
        {
            this.name = NamedServiceFactory<Codec>.GetServiceName(this.GetType());
        }

        /// <summary>
        /// Returns this codec's name </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Encodes/decodes postings </summary>
        public abstract PostingsFormat PostingsFormat { get; }

        /// <summary>
        /// Encodes/decodes docvalues </summary>
        public abstract DocValuesFormat DocValuesFormat { get; }

        /// <summary>
        /// Encodes/decodes stored fields </summary>
        public abstract StoredFieldsFormat StoredFieldsFormat { get; }

        /// <summary>
        /// Encodes/decodes term vectors </summary>
        public abstract TermVectorsFormat TermVectorsFormat { get; }

        /// <summary>
        /// Encodes/decodes field infos file </summary>
        public abstract FieldInfosFormat FieldInfosFormat { get; }

        /// <summary>
        /// Encodes/decodes segment info file </summary>
        public abstract SegmentInfoFormat SegmentInfoFormat { get; }

        /// <summary>
        /// Encodes/decodes document normalization values </summary>
        public abstract NormsFormat NormsFormat { get; }

        /// <summary>
        /// Encodes/decodes live docs </summary>
        public abstract LiveDocsFormat LiveDocsFormat { get; }

        /// <summary>
        /// looks up a codec by name </summary>
        public static Codec ForName(string name)
        {
            return codecFactory.GetCodec(name);
        }

        /// <summary>
        /// returns a list of all available codec names </summary>
        public static ICollection<string> AvailableCodecs()
        {
            if (codecFactory is IServiceListable)
            {
                return ((IServiceListable)codecFactory).AvailableServices();
            }
            else
            {
                throw new NotSupportedException("The current CodecFactory class does not implement IServiceListable.");
            }
        }

        // LUCENENET specific: Removed the ReloadCodecs() method because
        // this goes against the grain of standard DI practices.

        private static Codec defaultCodec;

        /// <summary>
        /// expert: returns the default codec used for newly created
        ///  <seealso cref="IndexWriterConfig"/>s.
        /// </summary>
        // TODO: should we use this, or maybe a system property is better?
        public static Codec Default
        {
            get
            {
                return defaultCodec;
            }
            set
            {
                defaultCodec = value;
            }
        }

        /// <summary>
        /// returns the codec's name. Subclasses can override to provide
        /// more detail (such as parameters).
        /// </summary>
        public override string ToString()
        {
            return name;
        }
    }

    ///// <summary>
    ///// Encodes/decodes an inverted index segment.
    ///// <p>
    ///// Note, when extending this class, the name (<seealso cref="#getName"/>) is
    ///// written into the index. In order for the segment to be read, the
    ///// name must resolve to your implementation via <seealso cref="#forName(String)"/>.
    ///// this method uses Java's
    ///// <seealso cref="ServiceLoader Service Provider Interface"/> (SPI) to resolve codec names.
    ///// <p>
    ///// If you implement your own codec, make sure that it has a no-arg constructor
    ///// so SPI can load it. </summary>
    ///// <seealso cref= ServiceLoader </seealso>
    //public abstract class Codec : NamedSPILoader.INamedSPI
    //{
    //    private static readonly NamedSPILoader<Codec> loader;

    //    private readonly string name;

    //    static Codec()
    //    {
    //        loader = new NamedSPILoader<Codec>(typeof(Codec));
    //        defaultCodec = Codec.ForName("Lucene46");
    //    }

    //    /// <summary>
    //    /// Creates a new codec.
    //    /// <p>
    //    /// The provided name will be written into the index segment: in order to
    //    /// for the segment to be read this class should be registered with Java's
    //    /// SPI mechanism (registered in META-INF/ of your jar file, etc). </summary>
    //    /// <param name="name"> must be all ascii alphanumeric, and less than 128 characters in length. </param>
    //    protected internal Codec(string name)
    //    {
    //        NamedSPILoader<Codec>.CheckServiceName(name);
    //        this.name = name;
    //    }

    //    /// <summary>
    //    /// Returns this codec's name </summary>
    //    public string Name
    //    {
    //        get
    //        {
    //            return name;
    //        }
    //    }

    //    /// <summary>
    //    /// Encodes/decodes postings </summary>
    //    public abstract PostingsFormat PostingsFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes docvalues </summary>
    //    public abstract DocValuesFormat DocValuesFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes stored fields </summary>
    //    public abstract StoredFieldsFormat StoredFieldsFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes term vectors </summary>
    //    public abstract TermVectorsFormat TermVectorsFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes field infos file </summary>
    //    public abstract FieldInfosFormat FieldInfosFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes segment info file </summary>
    //    public abstract SegmentInfoFormat SegmentInfoFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes document normalization values </summary>
    //    public abstract NormsFormat NormsFormat { get; }

    //    /// <summary>
    //    /// Encodes/decodes live docs </summary>
    //    public abstract LiveDocsFormat LiveDocsFormat { get; }

    //    /// <summary>
    //    /// looks up a codec by name </summary>
    //    public static Codec ForName(string name)
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called Codec.forName() before all Codecs could be initialized. " + "this likely happens if you call it from a Codec's ctor.");
    //        }
    //        return loader.Lookup(name);
    //    }

    //    /// <summary>
    //    /// returns a list of all available codec names </summary>
    //    public static ISet<string> AvailableCodecs()
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called Codec.AvailableCodecs() before all Codecs could be initialized. " +
    //                "this likely happens if you call it from a Codec's ctor.");
    //        }
    //        return loader.AvailableServices();
    //    }

    //    /// <summary>
    //    /// Reloads the codec list from the given <seealso cref="ClassLoader"/>.
    //    /// Changes to the codecs are visible after the method ends, all
    //    /// iterators (<seealso cref="#availableCodecs()"/>,...) stay consistent.
    //    ///
    //    /// <p><b>NOTE:</b> Only new codecs are added, existing ones are
    //    /// never removed or replaced.
    //    ///
    //    /// <p><em>this method is expensive and should only be called for discovery
    //    /// of new codecs on the given classpath/classloader!</em>
    //    /// </summary>
    //    public static void ReloadCodecs()
    //    {
    //        loader.Reload();
    //    }

    //    private static Codec defaultCodec;

    //    /// <summary>
    //    /// expert: returns the default codec used for newly created
    //    ///  <seealso cref="IndexWriterConfig"/>s.
    //    /// </summary>
    //    // TODO: should we use this, or maybe a system property is better?
    //    public static Codec Default
    //    {
    //        get
    //        {
    //            return defaultCodec;
    //        }
    //        set
    //        {
    //            defaultCodec = value;
    //        }
    //    }

    //    /// <summary>
    //    /// returns the codec's name. Subclasses can override to provide
    //    /// more detail (such as parameters).
    //    /// </summary>
    //    public override string ToString()
    //    {
    //        return name;
    //    }
    //}
}