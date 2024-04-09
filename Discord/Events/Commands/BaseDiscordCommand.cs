using Discord;
using Discord.WebSocket;

namespace ReactTracker.Discord.Events.Commands;

public abstract class BaseDiscordCommand
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract GuildPermission? MemberPermissions { get; }

    public abstract SlashCommandBuilder GetBuilder ( );
    public abstract Task Execute ( SocketSlashCommand command, SlashCommandHandler commandHandler);
}
