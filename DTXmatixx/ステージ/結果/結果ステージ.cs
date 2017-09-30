﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DirectInput;
using FDK;
using FDK.メディア;
using DTXmatixx.曲;
using DTXmatixx.アイキャッチ;

namespace DTXmatixx.ステージ.結果
{
	class 結果ステージ : ステージ
	{
		public enum フェーズ
		{
			表示,
			フェードアウト,
			確定,
		}
		public フェーズ 現在のフェーズ
		{
			get;
			protected set;
		}

		public 結果ステージ()
		{
			this.子リスト.Add( this._背景 = new 舞台画像() );
			this.子リスト.Add( this._曲名パネル = new 画像( @"$(System)images\結果画面_曲名パネル.png" ) );
			this.子リスト.Add( this._曲名画像 = new 文字列画像() {
				フォント名 = "HGMaruGothicMPRO",
				フォントサイズpt = 40f,
				フォント幅 = FontWeight.Regular,
				フォントスタイル = FontStyle.Normal,
				描画効果 = 文字列画像.効果.縁取り,
				縁のサイズdpx = 6f,
				前景色 = Color4.Black,
				背景色 = Color4.White,
			} );
		}

		protected override void On活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				var 選択曲 = App.曲ツリー.フォーカスノード as MusicNode;
				Debug.Assert( null != 選択曲 );

				this._曲名画像.表示文字列 = 選択曲.タイトル;

				this._プレビュー枠ブラシ = new SolidColorBrush( gd.D2DDeviceContext, new Color4( 0xFF209292 ) );
				this.現在のフェーズ = フェーズ.表示;
				this._初めての進行描画 = true;
			}
		}
		protected override void On非活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				FDKUtilities.解放する( ref this._プレビュー枠ブラシ );
			}
		}

		public override void 進行描画する( グラフィックデバイス gd )
		{
			if( this._初めての進行描画 )
			{
				this._背景.ぼかしと縮小を適用する( gd, 0.0 );	// 即時適用
				this._初めての進行描画 = false;
			}

			this._背景.進行描画する( gd );
			this._プレビュー画像を描画する( gd );
			this._曲名パネル.描画する( gd, 660f, 796f );
			this._曲名を描画する( gd );

			App.Keyboard.ポーリングする();

			switch( this.現在のフェーズ )
			{
				case フェーズ.表示:
					if( App.Keyboard.キーが押された( 0, Key.Return ) )
					{
						App.ステージ管理.アイキャッチを選択しクローズする( gd, nameof( シャッター ) );
						this.現在のフェーズ = フェーズ.フェードアウト;
					}
					break;

				case フェーズ.フェードアウト:
					App.ステージ管理.現在のアイキャッチ.進行描画する( gd );
					if( App.ステージ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
						this.現在のフェーズ = フェーズ.確定;
					break;

				case フェーズ.確定:
					break;
			}
		}

		private bool _初めての進行描画 = true;
		private 舞台画像 _背景 = null;
		private 画像 _曲名パネル = null;
		private 文字列画像 _曲名画像 = null;

		private SolidColorBrush _プレビュー枠ブラシ = null;
		private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 668f, 194f, 0f );
		private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 574f, 574f, 0f );

		private void _プレビュー画像を描画する( グラフィックデバイス gd )
		{
			var 選択曲 = App.曲ツリー.フォーカスノード as MusicNode;
			Debug.Assert( null != 選択曲 );

			var プレビュー画像 = 選択曲.ノード画像 ?? Node.既定のノード画像;
			Debug.Assert( null != プレビュー画像 );

			// 枠

			gd.D2DBatchDraw( ( dc ) => {
				const float 枠の太さdpx = 5f;
				dc.FillRectangle(
					new RectangleF(
						this._プレビュー画像表示位置dpx.X - 枠の太さdpx,
						this._プレビュー画像表示位置dpx.Y - 枠の太さdpx,
						this._プレビュー画像表示サイズdpx.X + 枠の太さdpx * 2f,
						this._プレビュー画像表示サイズdpx.Y + 枠の太さdpx * 2f ),
					this._プレビュー枠ブラシ );
			} );

			// テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。

			var 画面左上dpx = new Vector3(  // 3D視点で見る画面左上の座標。
				-gd.設計画面サイズ.Width / 2f,
				+gd.設計画面サイズ.Height / 2f,
				0f );

			var 変換行列 =
				Matrix.Scaling( this._プレビュー画像表示サイズdpx ) *
				Matrix.Translation(
					画面左上dpx.X + this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X / 2f,
					画面左上dpx.Y - this._プレビュー画像表示位置dpx.Y - this._プレビュー画像表示サイズdpx.Y / 2f,
					0f );

			プレビュー画像.描画する( gd, 変換行列 );
		}
		private void _曲名を描画する( グラフィックデバイス gd )
		{
			var 表示位置dpx = new Vector2( 690f, 820f );

			// 拡大率を計算して描画する。
			float 最大幅dpx = 555f;

			this._曲名画像.描画する(
				gd,
				表示位置dpx.X,
				表示位置dpx.Y,
				X方向拡大率: ( this._曲名画像.サイズ.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._曲名画像.サイズ.Width );
		}
	}
}
