namespace Roster.Application.Ports.Security;

public interface IProfanityFilter
{
    bool ContainsProfanity(string text);
}
