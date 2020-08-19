# -*- coding: utf-8 -*-
"""
Created on Fri Jul 31 11:00:13 2020

@author: chenliangtao
"""

import torch
import torch.nn as nn
import torch.nn.functional as F

BASE = 128
#1.5 * BASE
NODE_NUM = 192

class NetWork(nn.Module):
    
    def __init__(self):
        super(NetWork, self).__init__()
        self.fc1 = nn.Linear(BASE + BASE + 2, NODE_NUM)
        self.fc2 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc3 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc4 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc5 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc6 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc7 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc8 = nn.Linear(NODE_NUM, NODE_NUM)
        self.fc9 = nn.Linear(NODE_NUM, NODE_NUM)
        self.output = nn.Linear(NODE_NUM, BASE)
        
    def forward(self, x):
        x = F.relu(self.fc1(x))
        x = F.relu(self.fc2(x))
        x = F.relu(self.fc3(x))
        x = F.relu(self.fc4(x))
        x = F.relu(self.fc5(x))
        x = F.relu(self.fc6(x))
        x = F.relu(self.fc7(x))
        x = F.relu(self.fc8(x))
        x = F.relu(self.fc9(x))
        x = self.output(x)
        return x
        
            
    
    