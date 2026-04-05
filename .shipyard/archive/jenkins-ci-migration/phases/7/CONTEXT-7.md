# Phase 7: Replace Manual Threads — Design Decisions

## Decisions (from brainstorm)
- Replace new Thread(MainLoop) with Task.Run(() => MainLoop(), TaskCreationOptions.LongRunning) in PrimaryWorker and Worker
- All targets including net48 (Task.Run works fine on Framework)
- Replace Thread.Sleep(20) spin-wait in BaseMonitor.Cancel() with ManualResetEventSlim
- Adapt MultiWorkerBase.Running to check Task.IsCompleted instead of Thread.IsAlive
- Adapt WaitForThreadToFinish and StopThread to work with Task instead of Thread
