pipeline {
    agent none

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        NUGET_XMLDOC_MODE = 'skip'
    }

    stages {
        stage('Build & Unit Tests') {
            agent { label 'docker' }
            steps {
                sh 'dotnet restore "Source/DotNetWorkQueue.sln"'
                sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug --no-restore'

                sh '''
                    dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-core

                    dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-relational

                    dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-sqlserver

                    dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-postgresql

                    dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-redis

                    dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-sqlite

                    dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-litedb

                    dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-memory

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-dashboard-api

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Client.Tests/DotNetWorkQueue.Dashboard.Client.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-dashboard-client

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/unit-dashboard-ui
                '''

                stash includes: 'coverage/**/*.xml', name: 'unit-coverage'
            }
        }

        stage('Integration Tests') {
            parallel {
                stage('SqlServer') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 0, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN')]) {
                            sh 'echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlserver \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlserver'
                    }
                }

                stage('SqlServer Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 5, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN')]) {
                            sh 'echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlserver-linq \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlserver-linq'
                    }
                }

                stage('PostgreSQL') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 10, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                            sh 'echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-postgresql \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-postgresql'
                    }
                }

                stage('PostgreSQL Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 15, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                            sh 'echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-postgresql-linq \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-postgresql-linq'
                    }
                }

                stage('Redis') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 20, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')]) {
                            sh 'echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-redis \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-redis'
                    }
                }

                stage('Redis Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 25, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')]) {
                            sh 'echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-redis-linq \
                                -- --retry-failed-tests 1
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-redis-linq'
                    }
                }

                stage('SQLite') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 30, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlite
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlite'
                    }
                }

                stage('SQLite Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 35, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlite-linq
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlite-linq'
                    }
                }

                stage('LiteDB') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 40, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-litedb
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-litedb'
                    }
                }

                stage('LiteDB Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 45, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-litedb-linq
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-litedb-linq'
                    }
                }

                stage('Memory') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 50, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-memory
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-memory'
                    }
                }

                stage('Memory Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 55, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-memory-linq
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-memory-linq'
                    }
                }

                stage('Dashboard') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 60, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        withCredentials([
                            string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN'),
                            string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN'),
                            string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')
                        ]) {
                            sh '''
                                echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                                echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-postgresql.txt"
                                echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-redis.txt"
                            '''
                        }
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-dashboard
                        '''
                        stash includes: 'coverage/**/*.xml', name: 'cov-dashboard'
                    }
                }

                stage('TaskScheduler Distributed') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 65, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" \
                                -f net10.0 -c Debug
                        '''
                    }
                }

                stage('Dashboard UI E2E') {
                    // Uses Microsoft's Playwright .NET image which ships with Chromium
                    // and the .NET 8 SDK. The E2E test project targets net8.0 only.
                    agent {
                        docker {
                            image 'mcr.microsoft.com/playwright/dotnet:v1.54.0-noble'
                            args '--ipc=host'
                        }
                    }
                    steps {
                        sleep(time: 70, unit: 'SECONDS')
                        sh '''
                            dotnet build "Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/DotNetWorkQueue.Dashboard.Ui.E2E.Tests.csproj" -c Debug
                            dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/DotNetWorkQueue.Dashboard.Ui.E2E.Tests.csproj" \
                                --no-build -c Debug
                        '''
                    }
                }
            }
        }

        stage('Coverage Report') {
            agent { label 'docker' }
            steps {
                unstash 'unit-coverage'
                unstash 'cov-sqlserver'
                unstash 'cov-sqlserver-linq'
                unstash 'cov-postgresql'
                unstash 'cov-postgresql-linq'
                unstash 'cov-redis'
                unstash 'cov-redis-linq'
                unstash 'cov-sqlite'
                unstash 'cov-sqlite-linq'
                unstash 'cov-litedb'
                unstash 'cov-litedb-linq'
                unstash 'cov-memory'
                unstash 'cov-memory-linq'
                unstash 'cov-dashboard'

                withCredentials([string(credentialsId: 'reportgenerator-license', variable: 'REPORTGENERATOR_LICENSE')]) {
                    sh '''
                        dotnet tool install -g dotnet-reportgenerator-globaltool || true
                        export PATH="$PATH:$HOME/.dotnet/tools"

                        reportgenerator \
                            -reports:"coverage/**/coverage.cobertura.xml" \
                            -targetdir:coverage/report \
                            -reporttypes:"Html;Cobertura;Badges;TeamCitySummary" \
                            -license:"$REPORTGENERATOR_LICENSE"

                        echo "Merged coverage report generated at coverage/report/"
                    '''
                }

                withCredentials([string(credentialsId: 'codecov-token', variable: 'CODECOV_TOKEN')]) {
                    sh '''
                        curl -Os https://cli.codecov.io/latest/linux/codecov
                        chmod +x codecov
                        ./codecov upload-process --file coverage/report/Cobertura.xml --token "$CODECOV_TOKEN" || echo "Codecov upload failed (non-fatal)"
                    '''
                }

                publishHTML(target: [
                    allowMissing: false,
                    alwaysLinkToLastBuild: true,
                    keepAll: true,
                    reportDir: 'coverage/report',
                    reportFiles: 'index.html',
                    reportName: 'Code Coverage Report'
                ])
            }
        }
    }

    post {
        failure {
            echo 'Pipeline failed. Check stage logs for details.'
        }
        success {
            echo 'Pipeline completed successfully.'
        }
    }
}
