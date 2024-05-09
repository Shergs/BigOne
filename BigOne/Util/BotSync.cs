public static class BotSync
{
    public static SemaphoreSlim ReadySemaphore = new SemaphoreSlim(0, 1);
}