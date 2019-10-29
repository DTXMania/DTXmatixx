using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DTXMania2
{
    static class Program
    {
        public readonly static string _�r���A�[�p�p�C�v���C���� = "DTXMania2Viewer";

        [STAThread]
        static void Main( string[] args )
        {
            try
            {
                // ������

                timeBeginPeriod( 1 );
                Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );    // .NET Core �� Shift-JIS ���𗘗p�\�ɂ���

                #region " �R�}���h���C����������͂���B"
                //----------------
                Global.Options = new CommandLineOptions();

                if( !Global.Options.��͂���( args ) ) // ��͂Ɏ��s�����false
                {
                    // ���p�@��\�����ďI���B

                    Trace.WriteLine( Global.Options.Usage );             // Trace��
                    using( var console = new Console() )
                        console.Out?.WriteLine( Global.Options.Usage );  // �W���o�̗͂�����
                    return;
                }
                //----------------
                #endregion

                #region " ��d�N���`�F�b�N�܂��̓I�v�V�������M�B"
                //----------------
                using( var pipeToViewer = new NamedPipeClientStream( ".", _�r���A�[�p�p�C�v���C����, PipeDirection.Out ) )
                {
                    try
                    {
                        // �p�C�v���C���T�[�o�ւ̐ڑ������݂�B
                        pipeToViewer.Connect( 100 );

                        // (A) �T�[�r�X�������オ���Ă���
                        if( Global.Options.�r���A�[���[�h�ł��� )
                        {
                            #region " (A-a) �I�v�V�������e���T�[�o�֑��M���Đ���I���B"
                            //----------------
                            var ss = new StreamStringForNamedPipe( pipeToViewer );
                            var yamlText = Global.Options.ToYaml(); // YAML��
                            ss.WriteString( yamlText );
                            return;
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (A-b) ��d�N���Ƃ��ăG���[�I���B"
                            //----------------
                            var ss = new StreamStringForNamedPipe( pipeToViewer );
                            ss.WriteString( "ping" );

                            var msg = "��d�N���͂ł��܂���B";
                            Trace.WriteLine( msg );                     // Trace��
                            MessageBox.Show( msg, "DTXMania2 error" );  // �_�C�A���O�\���B
                            return;
                            //----------------
                            #endregion
                        }
                    }
                    catch( TimeoutException )
                    {
                        // (B) �T�[�r�X�������オ���Ă��Ȃ� �� ���̂܂܋N��
                    }
                }
                //----------------
                #endregion

                #region " AppData/DTXMania2 �t�H���_���Ȃ���΍쐬����B"
                //----------------
                //var AppData�t�H���_�� = Application.UserAppDataPath;  // %USERPROFILE%/AppData/<��Ж�>/DTXMania2/
                var AppData�t�H���_�� = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create ), "DTXMania2" ); // %USERPROFILE%/AppData/DTXMania2/

                if( !( Directory.Exists( AppData�t�H���_�� ) ) )
                    Directory.CreateDirectory( AppData�t�H���_�� );
                //----------------
                #endregion

                #region " ���O�t�@�C���ւ̃��O�̕����o�͊J�n�B"
                //----------------
                {
                    const int ���O�t�@�C���̍ő�ۑ����� = 30;
                    Trace.AutoFlush = true;

                    var ���O�t�@�C���� = Log.���O�t�@�C�����𐶐�����( Path.Combine( AppData�t�H���_��, "Logs" ), "Log.", TimeSpan.FromDays( ���O�t�@�C���̍ő�ۑ����� ) );

                    // ���O�t�@�C����Trace���X�i�Ƃ��Ēǉ��B
                    // �ȍ~�ATrace�i�Ȃ�т�Log�N���X�j�ɂ��o�͂́A���̃��X�i�i�����O�t�@�C���j�ɂ��o�͂����B
                    Trace.Listeners.Add( new TraceLogListener( new StreamWriter( ���O�t�@�C����, false, Encoding.GetEncoding( "utf-8" ) ) ) );

                    Log.���݂̃X���b�h�ɖ��O������( "Form" );
                }
                //----------------
                #endregion

                #region " �^�C�g���A���쌠�A�V�X�e���������O�o�͂���B"
                //----------------
                Log.WriteLine( $"{Application.ProductName} Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000}" );

                var copyrights = (AssemblyCopyrightAttribute[]) Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
                Log.WriteLine( $"{copyrights[ 0 ].Copyright}" );
                Log.WriteLine( "" );

                Log.�V�X�e���������O�o�͂���();
                Log.WriteLine( "" );
                //----------------
                #endregion

                #region " �t�H���_�ϐ���ݒ肷��B"
                //----------------
                {
                    var exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ?? "";

                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "Exe", exePath );
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "ResourcesRoot", Path.Combine( exePath, "Resources" ) );
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "DrumSounds", Path.Combine( exePath, @"Resources\Default\DrumSounds" ) );      // Skin.yaml �ɂ��ύX�����
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "SystemSounds", Path.Combine( exePath, @"Resources\Default\SystemSounds" ) );  // Skin.yaml �ɂ��ύX�����
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "Images", Path.Combine( exePath, @"Resources\Default\Images" ) );              // Skin.yaml �ɂ��ύX�����
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "AppData", AppData�t�H���_�� );
                    Folder.�t�H���_�ϐ���ǉ��܂��͍X�V����( "UserProfile", Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) );
                }
                //----------------
                #endregion


                // �A�v���N��

                Application.SetHighDpiMode( HighDpiMode.SystemAware );
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault( false );
                AppForm appForm;
                do
                {
                    appForm = new AppForm();
                    Application.Run( appForm );
                    appForm.Dispose();
                } while( appForm.�ċN�����K�v );  // �߂��Ă����ہA�ċN���t���O�������Ă����炱���ŃA�v�����ċN������B

                #region " ���l: �ċN���ɂ��� "
                //----------------
                // .NET Core 3 �� Application.Restart() ����ƁA�u�N�������v���Z�X����Ȃ��̂ŋp���v�ƌ�����B
                // �����炭�N���v���Z�X�� dotnet �ł��邽�߁H
                // �@
                // if( appForm.�ċN�����K�v )
                // {
                //     // ���ӁFVisual Sutdio �̃f�o�b�O����O�ݒ�� Common Language Runtime Exceptions �Ƀ`�F�b�N�����Ă���ƁA
                //     // ������ InvalidDeploymentException ���������ăf�o�b�K���ꎞ��~���邪�A����́u�t�@�[�X�g�`�����X��O�v�Ȃ̂ŁA
                //     // �P�ɖ������邱�ƁB
                //     Application.Restart();
                // }
                //----------------
                #endregion


                // �I��

                timeEndPeriod( 1 );

                Log.WriteLine( "" );
                Log.WriteLine( "�V��ł���Ă��肪�Ƃ��I" );
            }
#if !DEBUG
            // Release ���ɂ́A�������̗�O���L���b�`������_�C�A���O��\������B
            catch( Exception e )
            {
                MessageBox.Show(
                    $"�������̗�O���������܂����B\n\n" +
                    $"{e.Message}\n" +
                    $"{e.StackTrace}",
                    "Exception" );
            }
#else
            // Debug ���ɂ́A�������̗�O�����o����Ă������B�i�f�o�b�K�ŃL���b�`���邱�Ƃ�z��B�j
            finally
            {
            }
#endif
        }

        #region " Win32 "
        //----------------
        [System.Runtime.InteropServices.DllImport( "winmm.dll" )]
        static extern void timeBeginPeriod( uint x );

        [System.Runtime.InteropServices.DllImport( "winmm.dll" )]
        static extern void timeEndPeriod( uint x );
        //----------------
        #endregion
    }
}