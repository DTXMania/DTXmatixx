﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;
using DTXmatixx.データベース.ユーザ;
using DTXmatixx.ステージ.演奏;

namespace DTXmatixx.設定
{
	class ユーザ設定
	{
		public string ユーザID
		{
			get
				=> this._User.Id;
		}

		public string ユーザ名
		{
			get
				=> this._User.Name;
		}

		public bool 全画面モードである
		{
			get
				=> ( 0 != this._User.Fullscreen );
			set
				=> this._User.Fullscreen = value ? 1 : 0;
		}

		public double 譜面スクロール速度
		{
			get
				=> this._User.ScrollSpeed;
			set
				=> this._User.ScrollSpeed = value;
		}

		public bool AutoPlayがすべてONである
		{
			get
			{
				bool すべてON = true;

				foreach( var kvp in this.AutoPlay )
					すべてON &= kvp.Value;

				return すべてON;
			}
		}

		public HookedDictionary<AutoPlay種別, bool> AutoPlay
		{
			get;
			protected set;
		} = null;
		
		/// <summary>
		///		チップがヒット判定バーから（上または下に）どれだけ離れていると Perfect ～ Ok 判定になるのかの定義。秒単位。
		/// </summary>
		public HookedDictionary<判定種別, double> 最大ヒット距離sec { get; set; }

		public ドラムとチップと入力の対応表 ドラムとチップと入力の対応表
		{
			get;
			protected set;
		} = null;


		public ユーザ設定()
		{
			#region " User の初期化 "
			//----------------
			this._User = new User();
			//----------------
			#endregion
			#region " AutoPlay の初期化 "
			//----------------
			this.AutoPlay = new HookedDictionary<AutoPlay種別, bool>() {
				{ AutoPlay種別.Unknown, true },
				{ AutoPlay種別.LeftCrash, ( this._User.AutoPlay_LeftCymbal != 0 ) },
				{ AutoPlay種別.HiHat, ( this._User.AutoPlay_HiHat != 0 ) },
				{ AutoPlay種別.Foot, ( this._User.AutoPlay_LeftPedal != 0 ) },
				{ AutoPlay種別.Snare, ( this._User.AutoPlay_Snare != 0 ) },
				{ AutoPlay種別.Bass, ( this._User.AutoPlay_Bass != 0 ) },
				{ AutoPlay種別.Tom1, ( this._User.AutoPlay_HighTom != 0 ) },
				{ AutoPlay種別.Tom2, ( this._User.AutoPlay_LowTom != 0 ) },
				{ AutoPlay種別.Tom3, ( this._User.AutoPlay_FloorTom != 0 ) },
				{ AutoPlay種別.RightCrash, ( this._User.AutoPlay_RightCymbal != 0 ) },
			};

			// Dictionary が変更されたらDB用の個別プロパティも変更する。
			this.AutoPlay.get時アクション = null;
			this.AutoPlay.set時アクション = ( type, flag ) => {
				switch( type )
				{
					case AutoPlay種別.LeftCrash:
						this._User.AutoPlay_LeftCymbal = flag ? 1 : 0;
						break;

					case AutoPlay種別.HiHat:
						this._User.AutoPlay_HiHat = flag ? 1 : 0;
						break;

					case AutoPlay種別.Foot:
						this._User.AutoPlay_LeftPedal = flag ? 1 : 0;
						break;

					case AutoPlay種別.Snare:
						this._User.AutoPlay_Snare = flag ? 1 : 0;
						break;

					case AutoPlay種別.Bass:
						this._User.AutoPlay_Bass = flag ? 1 : 0;
						break;

					case AutoPlay種別.Tom1:
						this._User.AutoPlay_HighTom = flag ? 1 : 0;
						break;

					case AutoPlay種別.Tom2:
						this._User.AutoPlay_LowTom = flag ? 1 : 0;
						break;

					case AutoPlay種別.Tom3:
						this._User.AutoPlay_FloorTom = flag ? 1 : 0;
						break;

					case AutoPlay種別.RightCrash:
						this._User.AutoPlay_RightCymbal = flag ? 1 : 0;
						break;
				}
			};
			//----------------
			#endregion
			#region " 最大ヒット距離sec の初期化 "
			//----------------
			this.最大ヒット距離sec = new HookedDictionary<判定種別, double>() {
				{ 判定種別.PERFECT, this._User.MaxRange_Perfect },
				{ 判定種別.GREAT, this._User.MaxRange_Great },
				{ 判定種別.GOOD, this._User.MaxRange_Good },
				{ 判定種別.OK, this._User.MaxRange_Ok },
			};

			this.最大ヒット距離sec.get時アクション = null;
			this.最大ヒット距離sec.set時アクション = ( type, val ) => {
				switch( type )
				{
					case 判定種別.PERFECT:
						this._User.MaxRange_Perfect = val;
						break;

					case 判定種別.GREAT:
						this._User.MaxRange_Great = val;
						break;

					case 判定種別.GOOD:
						this._User.MaxRange_Good = val;
						break;

					case 判定種別.OK:
						this._User.MaxRange_Ok = val;
						break;

					case 判定種別.MISS:
						break;
				}
			};
			//----------------
			#endregion
			#region " ドラムとチップと入力の対応表の初期化 "
			//----------------
			this.ドラムとチップと入力の対応表 = new ドラムとチップと入力の対応表(
				new 表示レーンの左右() {    // 使わないので固定。
					Chinaは左 = false,
					Rideは左 = false,
					Splashは左 = true,
				} );
			//----------------
			#endregion
		}

		public ユーザ設定( string ユーザ名 )
			: this()
		{
			using( var userdb = new UserDB() )
			{
				var record = userdb.Users.Where( ( r ) => ( r.Name == ユーザ名 ) ).SingleOrDefault();

				if( null == record )
				{
					// (A) レコードが存在しないなら新規作成。
					this._User = new User();
				}
				else
				{
					// (B) レコードが存在すれば、その内容を継承する。
					this._User = record.Clone();
				}
			}
		}


		private User _User = null;
	}
}
