%% This file is used for visually analyze doppler frequency shift

close all
clear all
clc

%% load data and format data
format long g; 
fid = fopen('Data\Doppler.txt');
Info = textscan(fid, '%f %f %f ','Delimiter',',');
fclose(fid);
portNumber=Info{1};
DFS=Info{2};
Timestamp=Info{3};

windowSize=100000;
timeMax=ceil(max(Timestamp/windowSize));
Data=zeros(4,timeMax);

for t=1:timeMax
    for a=1:4
        indexTime=find(Timestamp>=((t-1)*windowSize) & Timestamp<=(t*windowSize));
        indexPort=find(portNumber==a);
        index= intersect(indexTime,indexPort);
        if (index~=0)
            Data(a,t)=mean(DFS(index));
        end
        
        clear index;
        clear indexPort;
        clear indexTime;
    end
end

imagesc(Data)