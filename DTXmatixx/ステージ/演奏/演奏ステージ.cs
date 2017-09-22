﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using FDK;
using FDK.メディア;
using FDK.カウンタ;
using SSTFormat.v2;
using DTXmatixx.設定;

namespace DTXmatixx.ステージ.演奏
{
	class 演奏ステージ : ステージ
	{
		public const float ヒット判定バーの中央Y座標dpx = 847f;

		public enum フェーズ
		{
			フェードイン,
			表示,
			クリア時フェードアウト,
			キャンセル,
		}
		public フェーズ 現在のフェーズ
		{
			get;
			protected set;
		}

		public Bitmap キャプチャ画面
		{
			get;
			set;
		} = null;

		public 演奏ステージ()
		{
			this.子リスト.Add( this._背景画像 = new 画像( @"$(System)images\演奏画面.png" ) );
			this.子リスト.Add( this._曲名パネル = new 曲名パネル() );
			this.子リスト.Add( this._ステータスパネル = new ステータスパネル() );
			this.子リスト.Add( this._成績パネル = new 成績パネル() );
			this.子リスト.Add( this._ヒットバー画像 = new 画像( @"$(System)images\演奏画面_ヒットバー.png" ) );
			this.子リスト.Add( this._ドラムパッド = new ドラムパッド() );
			this.子リスト.Add( this._ドラムチップ画像 = new 画像( @"$(System)images\ドラムチップ.png" ) );
			this.子リスト.Add( this._判定文字列 = new 判定文字列() );
			this.子リスト.Add( this._FPS = new FPS() );
		}

		protected override void On活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				this.キャプチャ画面 = null;
				this._描画開始チップ番号 = -1;
				this._小節線色 = new SolidColorBrush( gd.D2DDeviceContext, Color.White );
				this._拍線色 = new SolidColorBrush( gd.D2DDeviceContext, Color.LightGray );
				this._ドラムチップ画像の矩形リスト = new 矩形リスト( @"$(System)images\ドラムチップ矩形.xml" );      // デバイスリソースは持たないので、子Activityではない。
				this._現在進行描画中の譜面スクロール速度の倍率 = App.オプション設定.譜面スクロール速度の倍率;
				this._ドラムチップアニメ = new LoopCounter( 0, 200, 3 );
				this.現在のフェーズ = フェーズ.フェードイン;
				this._初めての進行描画 = true;
			}
		}
		protected override void On非活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				FDKUtilities.解放する( ref this._拍線色 );
				FDKUtilities.解放する( ref this._小節線色 );

				this.キャプチャ画面?.Dispose();
				this.キャプチャ画面 = null;
			}
		}

		public override void 高速進行する()
		{
			if( this._初めての進行描画 )
			{
				this._フェードインカウンタ = new Counter( 0, 100, 10 );
				this._初めての進行描画 = false;
			}

			// 高速進行

			this._FPS.FPSをカウントしプロパティを更新する();

			switch( this.現在のフェーズ )
			{
				case フェーズ.フェードイン:
					if( this._フェードインカウンタ.終了値に達した )
					{
						// フェードインが終わってから演奏開始。
						Log.Info( "演奏を開始します。" );
						this._描画開始チップ番号 = 0; // -1 から 0 に変われば演奏開始。
						App.サウンドタイマ.リセットする();

						this.現在のフェーズ = フェーズ.表示;
					}
					break;

				case フェーズ.表示:

					double 現在の演奏時刻sec = this._演奏開始からの経過時間secを返す();

					// AutoPlay 判定

					#region " 自動ヒット処理。"
					//----------------
					this._描画範囲のチップに処理を適用する( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離 ) => {

						var オプション設定 = App.オプション設定;
						var 対応表 = オプション設定.ドラムとチップと入力の対応表[ chip.チップ種別 ];
						var AutoPlay = オプション設定.AutoPlay[ 対応表.AutoPlay種別 ];

						bool チップはヒット済みである = chip.ヒット済みである;
						bool チップはMISSエリアに達している = ( ヒット判定バーと描画との時間sec > オプション設定.最大ヒット距離sec[ 判定種別.OK ] );
						bool チップは描画についてヒット判定バーを通過した = ( 0 <= ヒット判定バーと描画との時間sec );
						bool チップは発声についてヒット判定バーを通過した = ( 0 <= ヒット判定バーと発声との時間sec );

						if( チップはヒット済みである )
						{
							// 何もしない。
							return;
						}

						if( チップはMISSエリアに達している )
						{
							// MISS判定。
							if( AutoPlay && 対応表.AutoPlayON.MISS判定 )
							{
								this._チップのヒット処理を行う( chip, 判定種別.MISS, 対応表.AutoPlayON.自動ヒット時処理, ヒット判定バーと発声との時間sec );
								return;
							}
							else if( !AutoPlay && 対応表.AutoPlayOFF.MISS判定 )
							{
								this._チップのヒット処理を行う( chip, 判定種別.MISS, 対応表.AutoPlayOFF.ユーザヒット時処理, ヒット判定バーと発声との時間sec );
								return;
							}
							else
							{
								// 通過。
							}
						}

						if( チップは発声についてヒット判定バーを通過した )
						{
							// 自動ヒット判定。
							if( ( AutoPlay && 対応表.AutoPlayON.自動ヒット && 対応表.AutoPlayON.自動ヒット時処理.再生 ) ||
								( !AutoPlay && 対応表.AutoPlayOFF.自動ヒット && 対応表.AutoPlayOFF.自動ヒット時処理.再生 ) )
							{
								this._チップの発声を行う( chip, ヒット判定バーと発声との時間sec );
							}
						}

						if( チップは描画についてヒット判定バーを通過した )
						{
							// 自動ヒット判定。
							if( AutoPlay && 対応表.AutoPlayON.自動ヒット )
							{
								// Auto は Perfect 扱い
								this._チップのヒット処理を行う( chip, 判定種別.PERFECT, 対応表.AutoPlayON.自動ヒット時処理, ヒット判定バーと発声との時間sec );
								return;
							}
							else if( !AutoPlay && 対応表.AutoPlayOFF.自動ヒット )
							{
								// Auto は Perfect 扱い
								this._チップのヒット処理を行う( chip, 判定種別.PERFECT, 対応表.AutoPlayOFF.自動ヒット時処理, ヒット判定バーと発声との時間sec );
								return;
							}
							else
							{
								// 通過。
							}
						}

					} );
					//----------------
					#endregion


					// 入力

					App.Keyboard.ポーリングする();
					if( App.Keyboard.キーが押された( 0, Key.Escape ) )
					{
						#region " ESC → 演奏中断 "
						//----------------
						Log.Info( "演奏を中断します。" );
						this.現在のフェーズ = フェーズ.キャンセル;
						//----------------
						#endregion
					}
					if( App.Keyboard.キーが押された( 0, Key.Up ) )
					{
						#region " 上 → 譜面スクロールを加速 "
						//----------------
						const double 最大倍率 = 8.0;
						App.オプション設定.譜面スクロール速度の倍率 = Math.Min( App.オプション設定.譜面スクロール速度の倍率 + 0.5, 最大倍率 );
						//----------------
						#endregion
					}
					if( App.Keyboard.キーが押された( 0, Key.Down ) )
					{
						#region " 下 → 譜面スクロールを減速 "
						//----------------
						const double 最小倍率 = 0.5;
						App.オプション設定.譜面スクロール速度の倍率 = Math.Max( App.オプション設定.譜面スクロール速度の倍率 - 0.5, 最小倍率 );
						//----------------
						#endregion
					}

#warning "手動ヒット。"
					if( App.Keyboard.キーが押された( 0, Key.Z ) )
						this._判定文字列.表示を開始する( 表示レーン種別.LeftCrash, 判定種別.PERFECT );
					if( App.Keyboard.キーが押された( 0, Key.X ) )
						this._判定文字列.表示を開始する( 表示レーン種別.HiHat, 判定種別.GREAT );
					if( App.Keyboard.キーが押された( 0, Key.C ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Foot, 判定種別.GOOD );
					if( App.Keyboard.キーが押された( 0, Key.V ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Snare, 判定種別.OK );
					if( App.Keyboard.キーが押された( 0, Key.B ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Bass, 判定種別.MISS );
					if( App.Keyboard.キーが押された( 0, Key.N ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Tom1, 判定種別.PERFECT );
					if( App.Keyboard.キーが押された( 0, Key.M ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Tom2, 判定種別.GREAT );
					if( App.Keyboard.キーが押された( 0, Key.K ) )
						this._判定文字列.表示を開始する( 表示レーン種別.Tom3, 判定種別.GOOD );
					if( App.Keyboard.キーが押された( 0, Key.L ) )
						this._判定文字列.表示を開始する( 表示レーン種別.RightCrash, 判定種別.OK );
					break;

				case フェーズ.クリア時フェードアウト:
				case フェーズ.キャンセル:
					break;
			}
		}
		public override void 進行描画する( グラフィックデバイス gd )
		{
			// 進行描画

			if( this._初めての進行描画 )
				return; // まだ最初の高速進行が行われていない。

			switch( this.現在のフェーズ )
			{
				case フェーズ.フェードイン:
					{
						this._背景画像.描画する( gd, 0f, 0f );
						this._ドラムパッド.進行描画する( gd );
						this._ステータスパネル.描画する( gd );
						this._成績パネル.進行描画する( gd );
						this._曲名パネル.描画する( gd );
						this._ヒットバーを描画する( gd );
						this._キャプチャ画面を描画する( gd, ( 1.0f - this._フェードインカウンタ.現在値の割合 ) );
					}
					break;

				case フェーズ.表示:
					{
						#region " 譜面スクロール速度が変化している → 追い付き進行 "
						//----------------
						{
							double 倍率 = this._現在進行描画中の譜面スクロール速度の倍率;

							if( 倍率 < App.オプション設定.譜面スクロール速度の倍率 )
							{
								if( 0 > this._スクロール倍率追い付き用_最後の値 )
								{
									this._スクロール倍率追い付き用カウンタ = new LoopCounter( 0, 1000, 10 );    // 0→100; 全部で10×1000 = 10000ms = 10sec あれば十分だろう
									this._スクロール倍率追い付き用_最後の値 = 0;
								}
								else
								{
									while( this._スクロール倍率追い付き用_最後の値 < this._スクロール倍率追い付き用カウンタ.現在値 )
									{
										倍率 += 0.025;
										this._スクロール倍率追い付き用_最後の値++;
									}

									this._現在進行描画中の譜面スクロール速度の倍率 = Math.Min( 倍率, App.オプション設定.譜面スクロール速度の倍率 );
								}
							}
							else if( 倍率 > App.オプション設定.譜面スクロール速度の倍率 )
							{
								if( 0 > this._スクロール倍率追い付き用_最後の値 )
								{
									this._スクロール倍率追い付き用カウンタ = new LoopCounter( 0, 1000, 10 );    // 0→100; 全部で10×1000 = 10000ms = 10sec あれば十分だろう
									this._スクロール倍率追い付き用_最後の値 = 0;
								}
								else
								{
									while( this._スクロール倍率追い付き用_最後の値 < this._スクロール倍率追い付き用カウンタ.現在値 )
									{
										倍率 -= 0.025;
										this._スクロール倍率追い付き用_最後の値++;
									}

									this._現在進行描画中の譜面スクロール速度の倍率 = Math.Max( 倍率, App.オプション設定.譜面スクロール速度の倍率 );
								}
							}
							else
							{
								this._スクロール倍率追い付き用_最後の値 = -1;
								this._スクロール倍率追い付き用カウンタ = null;
							}
						}
						//----------------
						#endregion

						double 演奏時刻sec = this._演奏開始からの経過時間secを返す() + gd.次のDComp表示までの残り時間sec;

						this._小節線拍線を描画する( gd, 演奏時刻sec );
						this._背景画像.描画する( gd, 0f, 0f );
						this._ドラムパッド.進行描画する( gd );
						this._ステータスパネル.描画する( gd );
						this._成績パネル.進行描画する( gd );
						this._曲名パネル.描画する( gd );
						this._ヒットバーを描画する( gd );
						this._チップを描画する( gd, 演奏時刻sec );
						this._判定文字列.進行描画する( gd );
						this._FPS.VPSをカウントする();
						this._FPS.描画する( gd, 0f, 0f );
					}
					break;

				case フェーズ.クリア時フェードアウト:
				case フェーズ.キャンセル:
					break;
			}
		}

		private bool _初めての進行描画 = true;
		private 画像 _背景画像 = null;
		private 曲名パネル _曲名パネル = null;
		private ステータスパネル _ステータスパネル = null;
		private 成績パネル _成績パネル = null;
		private ドラムパッド _ドラムパッド = null;
		private 判定文字列 _判定文字列 = null;
		private FPS _FPS = null;
		/// <summary>
		///		読み込み画面: 0 ～ 1: 演奏画面
		/// </summary>
		private Counter _フェードインカウンタ = null;

		private double _現在進行描画中の譜面スクロール速度の倍率 = 1.0;
		private LoopCounter _スクロール倍率追い付き用カウンタ = null;
		private int _スクロール倍率追い付き用_最後の値 = -1;

		/// <summary>
		///		<see cref="スコア.チップリスト"/> のうち、描画を始めるチップのインデックス番号。
		///		未演奏時・演奏終了時は -1 。
		/// </summary>
		/// <remarks>
		///		演奏開始直後は 0 で始まり、対象番号のチップが描画範囲を流れ去るたびに +1 される。
		///		このメンバの更新は、高頻度進行タスクではなく、進行描画メソッドで行う。（低精度で構わないので）
		/// </remarks>
		private int _描画開始チップ番号 = -1;

		private double _演奏開始からの経過時間secを返す()
		{
			return App.サウンドタイマ.現在時刻sec;
		}

		/// <summary>
		///		<see cref="_描画開始チップ番号"/> から画面上端にはみ出すまでの間の各チップに対して、指定された処理を適用する。
		/// </summary>
		/// <param name="適用する処理">
		///		引数は、順に、対象のチップ, チップ番号, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx。
		///		時間と距離はいずれも、負数ならバー未達、0でバー直上、正数でバー通過。
		///	</param>
		private void _描画範囲のチップに処理を適用する( double 現在の演奏時刻sec, Action<チップ, int, double, double, double> 適用する処理 )
		{
			var スコア = App.演奏スコア;
			if( null == スコア )
				return;

			for( int i = this._描画開始チップ番号; ( 0 <= i ) && ( i < スコア.チップリスト.Count ); i++ )
			{
				var チップ = スコア.チップリスト[ i ];

				// ヒット判定バーとチップの間の、時間 と 距離 を算出。→ いずれも、負数ならバー未達、0でバー直上、正数でバー通過。
				double ヒット判定バーと描画との時間sec = 現在の演奏時刻sec - チップ.描画時刻sec;
				double ヒット判定バーと発声との時間sec = 現在の演奏時刻sec - チップ.発声時刻sec;
				double ヒット判定バーとの距離dpx = スコア.指定された時間secに対応する符号付きピクセル数を返す( this._現在進行描画中の譜面スクロール速度の倍率, ヒット判定バーと描画との時間sec );

				// 終了判定。
				bool チップは画面上端より上に出ている = ( ( ヒット判定バーの中央Y座標dpx + ヒット判定バーとの距離dpx ) < -40.0 );   // -40 はチップが隠れるであろう適当なマージン。
				if( チップは画面上端より上に出ている )
					break;

				// 処理実行。開始判定（描画開始チップ番号の更新）もこの中で。
				適用する処理( チップ, i, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx );
			}
		}

		private 画像 _ヒットバー画像 = null;
		private void _ヒットバーを描画する( グラフィックデバイス gd )
		{
			this._ヒットバー画像.描画する( gd, 441f, ヒット判定バーの中央Y座標dpx - 4f );    // 4f がバーの厚みの半分[dpx]。
		}

		// 小節線・拍線 と チップ は描画階層（奥行き）が異なるので、別々のメソッドに分ける。
		private SolidColorBrush _小節線色 = null;
		private SolidColorBrush _拍線色 = null;
		private void _小節線拍線を描画する( グラフィックデバイス gd, double 現在の演奏時刻sec )
		{
			gd.D2DBatchDraw( ( dc ) => {

				this._描画範囲のチップに処理を適用する( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx ) => {

					if( chip.チップ種別 == チップ種別.小節線 )
					{
						float 上位置dpx = (float) ( ヒット判定バーの中央Y座標dpx + ヒット判定バーとの距離dpx - 1f );   // -1f は小節線の厚みの半分。
						dc.DrawLine( new Vector2( 441f, 上位置dpx ), new Vector2( 441f + 780f, 上位置dpx ), this._小節線色, strokeWidth: 3f );
					}
					else if( chip.チップ種別 == チップ種別.拍線 )
					{
						float 上位置dpx = (float) ( ヒット判定バーの中央Y座標dpx + ヒット判定バーとの距離dpx - 1f );   // -1f は拍線の厚みの半分。
						dc.DrawLine( new Vector2( 441f, 上位置dpx ), new Vector2( 441f + 780f, 上位置dpx ), this._拍線色, strokeWidth: 1f );
					}

				} );

			} );
		}

		private 画像 _ドラムチップ画像 = null;
		private 矩形リスト _ドラムチップ画像の矩形リスト = null;
		private LoopCounter _ドラムチップアニメ = null;
		private void _チップを描画する( グラフィックデバイス gd, double 現在の演奏時刻sec )
		{
			Debug.Assert( null != this._ドラムチップ画像の矩形リスト );

			this._描画範囲のチップに処理を適用する( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx ) => {

				float 縦中央位置dpx = (float) ( ヒット判定バーの中央Y座標dpx + ヒット判定バーとの距離dpx );

				// チップがヒット判定バーを通過してたら、通過距離に応じて 0→1の消滅割合を付与する。
				// 0で完全表示、1で完全消滅、通過してなければ 0。
				const float 消滅を開始するヒット判定バーからの距離dpx = 20f;
				const float 消滅開始から完全消滅するまでの距離dpx = 70f;
				float 消滅割合 = 0f;
				if( 消滅を開始するヒット判定バーからの距離dpx < ヒット判定バーとの距離dpx )
				{
					消滅割合 = Math.Min( 1f, (float) ( ( ヒット判定バーとの距離dpx - 消滅を開始するヒット判定バーからの距離dpx ) / 消滅開始から完全消滅するまでの距離dpx ) );
				}

				#region " チップが描画開始チップであり、かつ、そのY座標が画面下端を超えたなら、描画開始チップ番号を更新する。"
				//----------------
				if( ( index == this._描画開始チップ番号 ) &&
					( gd.設計画面サイズ.Height + 40.0 < 縦中央位置dpx ) )   // +40 はチップが隠れるであろう適当なマージン。
				{
					this._描画開始チップ番号++;

					if( App.演奏スコア.チップリスト.Count <= this._描画開始チップ番号 )
					{
						this.現在のフェーズ = フェーズ.クリア時フェードアウト;
						this._描画開始チップ番号 = -1;    // 演奏完了。
						return;
					}
				}
				//----------------
				#endregion

				if( chip.不可視 )
					return;

				float 音量0to1 = 1f;      // chip.音量 / (float) チップ.最大音量;		matixx では音量無視。

				var lane = App.システム設定.チップto表示レーン[ chip.チップ種別 ];
				if( lane != 表示レーン種別.Unknown )
				{
					// xml の記述ミスの検出用。
					Debug.Assert( null != this._ドラムチップ画像の矩形リスト[ lane.ToString() ] );
					Debug.Assert( null != this._ドラムチップ画像の矩形リスト[ lane.ToString() + "_back" ] );

					var 縦方向中央位置dpx = this._ドラムチップ画像の矩形リスト[ "縦方向中央位置" ]?.Height ?? 0f;

					#region " パッド絵 "
					//----------------
					{
						var 矩形 = this._ドラムチップ画像の矩形リスト[ lane.ToString() + "_back" ].Value;
						var 矩形中央 = new Vector2( 矩形.Width / 2f, 矩形.Height / 2f );
						var 割合 = this._ドラムチップアニメ.現在値の割合;   // 0→1のループ

						var 変換行列2D = ( 0 >= 消滅割合 ) ? Matrix3x2.Identity : Matrix3x2.Scaling( 1f - 消滅割合, 1f, 矩形中央 );

						// 拡大縮小回転
						switch( lane )
						{
							case 表示レーン種別.LeftCrash:
							case 表示レーン種別.HiHat:
							case 表示レーン種別.Foot:
							case 表示レーン種別.Tom3:
							case 表示レーン種別.RightCrash:
								{
									float v = (float) ( Math.Sin( 2 * Math.PI * 割合 ) * 0.2 );
									変換行列2D = 変換行列2D * Matrix3x2.Scaling( (float) ( 1 + v ), (float) ( 1 - v ) * 音量0to1, 矩形中央 );
								}
								break;

							case 表示レーン種別.Bass:
								{
									float r = (float) ( Math.Sin( 2 * Math.PI * 割合 ) * 0.2 );
									変換行列2D = 変換行列2D *
										Matrix3x2.Scaling( 1f, 音量0to1, 矩形中央 ) *
										Matrix3x2.Rotation( (float) ( r * Math.PI ), 矩形中央 );
								}
								break;

							default:
								変換行列2D = 変換行列2D * Matrix3x2.Scaling( 1f, 音量0to1, 矩形中央 );
								break;
						}

						// 移動
						変換行列2D = 変換行列2D *
							Matrix3x2.Translation(
								x: レーンフレーム.領域.Left + レーンフレーム.レーンto左端位置dpx[ lane ],
								y: 縦中央位置dpx - 縦方向中央位置dpx * 音量0to1 );

						this._ドラムチップ画像.描画する(
							gd,
							変換行列2D,
							転送元矩形: 矩形,
							不透明度0to1: 1f - 消滅割合 );
					}
					//----------------
					#endregion

					#region " チップ本体 "
					//----------------
					{
						var 矩形 = this._ドラムチップ画像の矩形リスト[ lane.ToString() ].Value;
						var 矩形中央 = new Vector2( 矩形.Width / 2f, 矩形.Height / 2f );

						var 変換行列2D =
							( ( 0 >= 消滅割合 ) ? Matrix3x2.Identity : Matrix3x2.Scaling( 1f - 消滅割合, 1f, 矩形中央 ) ) *
							Matrix3x2.Scaling( 1f, 音量0to1, 矩形中央 ) *
							Matrix3x2.Translation(
								x: レーンフレーム.領域.Left + レーンフレーム.レーンto左端位置dpx[ lane ],
								y: 縦中央位置dpx - 縦方向中央位置dpx * 音量0to1 );

						this._ドラムチップ画像.描画する(
							gd,
							変換行列2D,
							転送元矩形: 矩形,
							不透明度0to1: 1f - 消滅割合 );
					}
					//----------------
					#endregion
				}

			} );
		}

		private void _チップのヒット処理を行う( チップ chip, 判定種別 judge, ドラムとチップと入力の対応表.Column.Columnヒット処理 ヒット処理表, double ヒット判定バーと発声との時間sec )
		{
			chip.ヒット済みである = true;

			if( ヒット処理表.再生 )
			{
				#region " チップの発声を行う。"
				//----------------
				if( chip.発声されていない )
					this._チップの発声を行う( chip, ヒット判定バーと発声との時間sec );
				//----------------
				#endregion
			}
			if( ヒット処理表.判定 )
			{
				#region " チップの判定処理を行う。"
				//----------------
				var 対応表 = App.オプション設定.ドラムとチップと入力の対応表[ chip.チップ種別 ];

				if( judge != 判定種別.MISS )
				{
					// (A) PERFECT～POOR

					//this._コンボ.COMBO値++;

					//this._回転羽.発火する(
					//	new Vector2(
					//		レーンフレームの左端位置 + レーンフレーム.レーンto横中央相対位置[ 対応表.表示レーン種別 ],
					//		ヒット判定バーの中央Y座標 ) );

					//this._ドラムセット.ヒットアニメ開始( 対応表.ドラム入力種別, App.ユーザ管理.選択されているユーザ.オプション設定.表示レーンの左右 );

					//if( hitRankType == ヒットランク種別.AUTO )
					//	this._レーンフレーム.フラッシュ開始( 対応表.表示レーン種別 );   // レーンフラッシュは Auto 時のみ。

					this._判定文字列.表示を開始する( 対応表.表示レーン種別, judge );
					//this.ヒットランク別ヒット回数[ hitRankType ]++;
				}
				else
				{
					// (B) MISS

					//this._コンボ.COMBO値 = 0;

					this._判定文字列.表示を開始する( 対応表.表示レーン種別, 判定種別.MISS );
					//this.ヒットランク別ヒット回数[ hitRankType ]++;
				}
				//----------------
				#endregion
			}
			if( ヒット処理表.非表示 )
			{
				#region " チップを非表示にする。"
				//----------------
				if( judge != 判定種別.MISS )
				{
					chip.可視 = false;        // PERFECT～POOR チップは非表示。
				}
				else
				{
					// MISSチップは最後まで表示し続ける。
				}
				//----------------
				#endregion
			}
		}
		private void _チップの発声を行う( チップ chip, double 再生開始位置sec )
		{
			if( chip.発声済みである )
				return;

			chip.発声済みである = true;

			//if( chip.チップ種別 == チップ種別.背景動画 )
			//{
			//	App.サウンドタイマ.一時停止する();

			//	// 背景動画の再生を開始する。
			//	this._背景動画?.再生を開始する();
			//	this._背景動画開始済み = true;

			//	// BGMの再生を開始する。
			//	this._BGM?.Play( 再生開始位置sec );
			//	this._BGM再生開始済み = true;

			//	App.サウンドタイマ.再開する();
			//}
			//else
			//{
			//	// BGM以外のサウンドについては、再生開始位置sec は反映せず、常に最初から再生する。
			//	if( App.システム設定.Autoチップのドラム音を再生する )
			//		this._ドラムサウンド.発声する( chip.チップ種別, ( chip.音量 / (float) チップ.最大音量 ) );
			//}
		}

		private void _キャプチャ画面を描画する( グラフィックデバイス gd, float 不透明度 = 1.0f )
		{
			Debug.Assert( null != this.キャプチャ画面, "キャプチャ画面が設定されていません。" );

			gd.D2DBatchDraw( ( dc ) => {
				dc.DrawBitmap(
					this.キャプチャ画面,
					new RectangleF( 0f, 0f, gd.設計画面サイズ.Width, gd.設計画面サイズ.Height ),
					不透明度,
					BitmapInterpolationMode.Linear );
			} );
		}
	}
}
