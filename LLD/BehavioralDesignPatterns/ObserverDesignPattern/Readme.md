Observer Pattern

Tight coupling: The subject (publisher) knows its observers directly.

Communication is usually in-process, inside the same application.

Observers subscribe directly to the subject object.

the Observer Pattern has two variants:
🎯 1. PUSH Model
🎯 2. PULL Model
Let’s break them down very clearly with simple examples.

1. PUSH Model (Publisher pushes all data to observers)
✔ Concept
•	The Subject sends the full updated data directly to the Observer.
•	Observer doesn’t need to request anything.
•	More overhead if the data is large or observers don’t need everything.
✔ Real-life analogy
You subscribe to news notifications →
The news app sends the entire article as a notification.
Even if you only needed the headline.
✔ Pros
•	Fast notifications
•	Observer gets everything immediately
✔ Cons
•	Observers may get data they don’t need
•	Higher network/CPU load

1. Real-World Example: Fitness Tracker (Heart Rate Observer)
Scenario:

A FitnessTracker device measures heart rate.
Observers:

MobileApp → Shows your current BPM

HealthAlertService → Warns if BPM too high

CloudSyncService → Uploads BPM data when needed

This is a very realistic use case.

                      +-----------------------+
                      |      ISubject         |
                      |-----------------------|
                      | + Attach(obs)         |
                      | + Detach(obs)         |
                      | + Notify()            |
                      +-----------+-----------+
                                  ^
                                  |
                                  |
                    +-------------+--------------+
                    |        FitnessTracker      |
                    |----------------------------|
                    | - observers : List<IObserver> 
                    | - heartRate : int         |
                    |----------------------------|
                    | + SetHeartRate(int)       |
                    | + GetHeartRate()          |
                    | + Attach() / Detach()     |
                    | + Notify()                |
                    +-------------+--------------+
                                  |
                                  |
                     +------------+-----------+
                     |                        |
        +-------------+-----------+   +--------+----------------+
        |        IObserver        |   |      PullObserver       |
        |--------------------------|   |--------------------------|
        | + Update(data?)         |   | + Update(subjectRef)     |
        +-------------+-----------+   +-----------+--------------+
                      ^                           ^
                      |                           |
        +-------------+--------------+   +--------+----------------+
        | MobileApp (Push)           |   | HealthAlertService      |
        | CloudSyncService (Push)    |   | MobileApp (Pull)        |
        |----------------------------|   |--------------------------|
