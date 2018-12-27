﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSCore;
using CSCore.Codecs.WAV;

namespace FDK
{
    /// <summary>
    ///		指定されたメディアファイルを PCM WAVE としてデコードして、<see cref="CSCore.IWaveAggregator"/> オブジェクトを生成する。
    ///		リサンプラーなし版。
    /// </summary>
    public class WavOnMemoryWaveSource : IWaveSource
    {
        public bool CanSeek => this._WaveFileReader.CanSeek;

        public WaveFormat WaveFormat => this._WaveFileReader.WaveFormat;

        /// <summary>
        ///		現在の再生位置[byte]。
        /// </summary>
        public long Position
        {
            get => this._WaveFileReader.Position;
            set => this._WaveFileReader.Position = FDKUtilities.位置をブロック境界単位にそろえて返す( value, this.WaveFormat.BlockAlign );
        }

        /// <summary>
        ///		デコード後のオーディオデータのすべての長さ[byte]。
        /// </summary>
        public long Length => this._WaveFileReader.Length;


        public WavOnMemoryWaveSource( VariablePath ファイルパス, WaveFormat deviceFormat )
        {
            this._WaveFileReader = new WaveFileReader( ファイルパス.変数なしパス );
        }

        /// <summary>
        ///		連続したデータを読み込み、<see cref="Position"/> を読み込んだ数だけ進める。
        /// </summary>
        /// <param name="buffer">読み込んだデータを格納するための配列。</param>
        /// <param name="offset"><paramref name="buffer"/> に格納を始める位置。</param>
        /// <param name="count">読み込む最大のデータ数。</param>
        /// <returns><paramref name="buffer"/> に読み込んだデータの総数。</returns>
        public int Read( byte[] buffer, int offset, int count )
            => this._WaveFileReader.Read( buffer, offset, count );

        public void Dispose()
        {
            this._WaveFileReader?.Dispose();
            this._WaveFileReader = null;
        }


        private WaveFileReader _WaveFileReader = null;
    }
}
