﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2.old.RecordDBRecord
{
    class v007_RecordDBRecord
    {
        public const int VERSION = 7;

        /// <summary>
        ///		ユーザを一意に識別するID。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///		曲譜面ファイルのハッシュ値。
        /// </summary>
        public string SongHashId { get; set; }

        /// <summary>
        ///		スコア。
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///		カウントマップラインのデータ。
        ///		１ブロックを１文字（'0':0～'C':12）で表し、<see cref="DTXMania.ステージ.演奏.カウントマップライン.カウントマップの最大要素数"/> 個の文字が並ぶ。
        ///		もし不足分があれば、'0' とみなされる。
        /// </summary>
        public string CountMap { get; set; }

        /// <summary>
        ///		達成率。
        /// </summary>
        public double Achievement { get; set; }


        /// <summary>
        ///		テーブルのカラム部分を列挙したSQL。
        /// </summary>
        public static readonly string ColumnList =
            @"( UserId NVARCHAR NOT NULL" +
            @", SongHashId NVARCHAR NOT NULL" +
            @", Score INTEGER NOT NULL" +
            @", CountMap NVARCHAR NOT NULL" +
            @", Achievement NUMERIC NOT NULL" +
            @", PRIMARY KEY(`UserId`,`SongHashId`)" +
            @")";



        // 生成と終了

        public v007_RecordDBRecord()
        {
            this.UserId = "Anonymous";
            this.SongHashId = "";
            this.Score = 0;
            this.CountMap = "";
            this.Achievement = 0.0;
        }

        public v007_RecordDBRecord( SqliteDataReader reader )
            : this()
        {
            this.UpdateFrom( reader );
        }

        /// <summary>
        ///     SqliteDataReader からレコードを読み込んでフィールドを更新する。
        /// </summary>
        /// <param name="record">Read() 済みの SqliteDataReader。</param>
        public void UpdateFrom( SqliteDataReader record )
        {
            for( int i = 0; i < record.FieldCount; i++ )
            {
                switch( record.GetName( i ) )
                {
                    case "UserId": this.UserId = record.GetString( i ); break;
                    case "SongHashId": this.SongHashId = record.GetString( i ); break;
                    case "Score": this.Score = record.GetInt32( i ); break;
                    case "CountMap": this.CountMap = record.GetString( i ); break;
                    case "Achievement": this.Achievement = record.GetDouble( i ); break;
                }
            }
        }

        /// <summary>
        ///     DBにレコードを挿入または更新する。
        /// </summary>
        public void InsertTo( SQLiteDB db )
        {
            using var cmd = new SqliteCommand(
                "REPLACE INTO Records VALUES(" +
                $"'{this.UserId}'," +
                $"'{this.SongHashId}'," +
                $"{this.Score}," +
                $"'{this.CountMap}'," +
                $"{this.Achievement}" +
                ")", db.Connection );

            cmd.ExecuteNonQuery();
        }
    }
}
