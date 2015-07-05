using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;

namespace ChanArchiver
{
    public static class ArchivedThreadAdder
    {
        public static AddThreadFromArchiveStatus AddThreadFromArchive(string board, int tid, bool outputConsoleMessages = false, ArchiveInfo info = null)
        {
            if (info == null)
            {
                var archives = ArchivesProvider.GetArchivesForBoard(board, false);

                if (archives.Length == 0)
                {
                    if (outputConsoleMessages)
                    {
                        Console.WriteLine("Cannot find an archive to use for board {0}", board);
                    }
                    return AddThreadFromArchiveStatus.CannotFindAnArchive;

                }
                else
                {
                    if (outputConsoleMessages)
                    {
                        Console.WriteLine("Found {0} archives for board {1}", archives.Length, board);
                    }

                    foreach (var archive_info in archives)
                    {
                        if (archive_info.Software == ArchiveInfo.ArchiverSoftware.FoolFuuka)
                        {
                            info = archive_info;
                            if (outputConsoleMessages)
                            {
                                Console.WriteLine("Selecting {0} (host: {1}) archive", info.Name, info.Domain);
                            }
                            break;
                        }
                    }

                }
            }

            bool is_4chan_board = Program.ValidBoards.ContainsKey(board);
            bool archive_found = info != null;

            if (is_4chan_board && archive_found)
            {
                FoolFuukaParserData a = new FoolFuukaParserData(info, board, tid);

                BoardWatcher bw = Program.GetBoardWatcher(board);

                if (outputConsoleMessages)
                {
                    Console.WriteLine("Adding thread {0} from board {1}...", a.ThreadID, a.BOARD);
                }

                ThreadContainer tc = FoolFuukaParser.Parse(a);

                if (tc != null)
                {
                    bw.AddStaticThread(tc, Settings.ThumbnailOnly);
                    if (outputConsoleMessages)
                    {
                        Console.WriteLine("Thread {0} from board {1} added.", a.ThreadID, a.BOARD);
                    }
                    return AddThreadFromArchiveStatus.Success;
                }
                else
                {
                    if (outputConsoleMessages)
                    {
                        Console.WriteLine("Cannot add this thread. Possible reasons:\n"
                            + "- The thread ID is invalid\n"
                            + "- The archive no longer archive this board.\n"
                            + "- The archive has no JSON API support");
                    }
                    return AddThreadFromArchiveStatus.Error;
                }
            }
            else if (!is_4chan_board)
            {
                if (outputConsoleMessages)
                {
                    Console.WriteLine("Unsupported board {0}", board);
                }
                return AddThreadFromArchiveStatus.UnsupportedBoard;
            }
            else if (!archive_found)
            {
                if (outputConsoleMessages)
                {
                    Console.WriteLine("Cannot find an archive with supported software");
                }
                return AddThreadFromArchiveStatus.UnsupportedSoftware;
            }
            return AddThreadFromArchiveStatus.Unknown;
        }
    }


    public enum AddThreadFromArchiveStatus
    {
        CannotFindAnArchive, Error, UnsupportedBoard, UnsupportedSoftware, Success, Unknown
    }
}
